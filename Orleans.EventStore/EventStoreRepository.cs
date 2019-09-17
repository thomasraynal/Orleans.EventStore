using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    //remove connection monitor...
    public class EventStoreRepository : IEventStoreRepository
    {
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly IEventStoreRepositoryConfiguration _eventStoreRepositoryConfiguration;
        private readonly IConnectionStatusMonitor _connectionMonitor;
        private bool _isStarted;

        public bool IsConnected => _connectionMonitor.IsConnected;
        public bool IStarted => _isStarted;

        public static EventStoreRepository Create(IEventStoreRepositoryConfiguration repositoryConfiguration)
        {
            var eventStoreConnection = new ExternalEventStore(repositoryConfiguration.ConnectionString, repositoryConfiguration.ConnectionSettings).Connection;
            var repository = new EventStoreRepository(repositoryConfiguration, eventStoreConnection, new ConnectionStatusMonitor(eventStoreConnection));
            return repository;
        }

        private EventStoreRepository(IEventStoreRepositoryConfiguration configuration, IEventStoreConnection eventStoreConnection, IConnectionStatusMonitor connectionMonitor)
        {
            _eventStoreRepositoryConfiguration = configuration;
            _eventStoreConnection = eventStoreConnection;
            _connectionMonitor = connectionMonitor;
        }

        public async Task Connect(TimeSpan timeout)
        {
            if (_isStarted) return;

            _isStarted = true;

            await _connectionMonitor.Connect();
            await Wait.Until(() => IsConnected, timeout);
        }

        public async Task CreatePersistentSubscription(string streamId, string group)
        {
 
            var persistentSubscriptionSettings = PersistentSubscriptionSettings.Create().DoNotResolveLinkTos().StartFromCurrent();

            await _eventStoreConnection.CreatePersistentSubscriptionAsync(streamId, group, persistentSubscriptionSettings, _eventStoreRepositoryConfiguration.UserCredentials);

        }

        public IObservable<IEvent> ObservePersistentSubscription(string streamId, string group)
        {
            return SubscribeToPersistentSubscription(streamId, group)
                    .TakeUntil((_) => !IsConnected)
                    .Retry();
        }

        //todo: handle ack
        //todo: check arguments
        private IObservable<IEvent> SubscribeToPersistentSubscription(string streamId, string group)
        {
            return Observable.Create<IEvent>(async (obs) =>
            {
                var persistentSubscription = await _eventStoreConnection.ConnectToPersistentSubscriptionAsync(streamId, group, (eventStoreSubscription, resolvedEvent) =>
                {
                    var @event = DeserializeEvent(resolvedEvent.Event);

                    @event.Version = resolvedEvent.Event.EventNumber;

                    obs.OnNext(@event);

                }, (subscription, reason, exception) =>
                {

                    if (null != exception) obs.OnError(exception);
                    else
                    {
                        obs.OnError(new Exception($"{reason}"));
                    }

                }, _eventStoreRepositoryConfiguration.UserCredentials, _eventStoreRepositoryConfiguration.BufferSize, _eventStoreRepositoryConfiguration.AutoAck);


                return Disposable.Create(() =>
                {
                    persistentSubscription.Stop(_eventStoreRepositoryConfiguration.ConnectionClosedTimeout);
                });

            });
        }

        public IObservable<IEvent> Observe(string streamId, long? fromIncluding = null, bool rewindAfterDisconnection = false)
        {
            return CreateSubscription(streamId, fromIncluding, rewindAfterDisconnection)
                 .TakeUntil((_) => !IsConnected)
                 .Retry();
        }

        public async Task<IEnumerable<IEvent>> GetStream(string streamId)
        {
            if (!IsConnected) throw new InvalidOperationException("not connected");

            StreamEventsSlice currentSlice;

            var result = new List<IEvent>();

            do
            {
                currentSlice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamId, StreamPosition.Start, _eventStoreRepositoryConfiguration.ReadPageSize, false);

                if (currentSlice.Status == SliceReadStatus.StreamNotFound || currentSlice.Status == SliceReadStatus.StreamDeleted)
                {
                    break;
                }


                foreach (var resolvedEvent in currentSlice.Events)
                {
                    var @event = DeserializeEvent(resolvedEvent.Event);

                    result.Add(@event);
                }

            } while (!currentSlice.IsEndOfStream);


            return result;
        }

        //todo: handle usercredentials
        public async Task<StreamMetadataResult> GetStreamMetadata(string streamId)
        {
            return await _eventStoreConnection.GetStreamMetadataAsync(streamId);
        }

        public async Task<(int version, TState aggregate)> GetAggregate<TKey, TState>(TKey id) where TState : class, IAggregate, new()
        {
            if (!IsConnected) throw new InvalidOperationException("not connected");

            var streamName = $"{id}";

            var eventNumber = 0L;

            var aggregate = new TState();

            StreamEventsSlice currentSlice;
           
            do
            {
                currentSlice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, eventNumber, _eventStoreRepositoryConfiguration.ReadPageSize, false);

                if (currentSlice.Status == SliceReadStatus.StreamNotFound || currentSlice.Status == SliceReadStatus.StreamDeleted)
                {
                    break;
                }

                eventNumber = currentSlice.NextEventNumber;

                foreach (var resolvedEvent in currentSlice.Events)
                {
                    var @event = DeserializeEvent(resolvedEvent.Event);

                    aggregate.Apply(@event);

                }

            } while (!currentSlice.IsEndOfStream);


            return ((int)eventNumber, aggregate);
        }

        public async Task SavePendingEvents(string streamId, long originalVersion, IEnumerable<IEvent> pendingEvents, params KeyValuePair<string, string>[] extraHeaders)
        {

            WriteResult result;

            var commitHeaders = CreateCommitHeaders(extraHeaders);
            var eventsToSave = pendingEvents.Select(ev => ToEventData(ev, commitHeaders));

            var eventBatches = GetEventBatches(eventsToSave);

            if (eventBatches.Count == 1)
            {
                result = await _eventStoreConnection.AppendToStreamAsync(streamId, originalVersion, eventBatches[0]);
            }
            else
            {
                using (var transaction = await _eventStoreConnection.StartTransactionAsync(streamId, originalVersion))
                {
                    foreach (var batch in eventBatches)
                    {
                        await transaction.WriteAsync(batch);
                    }

                    result = await transaction.CommitAsync();
                }
            }

        }

        private IObservable<IEvent> CreateSubscription(string streamId, long? fromIncluding, bool rewindfAfterDisconnection)
        {
            return Observable.Create<IEvent>(async (obs) =>
            {
                return  await CreateEventStoreSubscription(streamId, obs, fromIncluding, rewindfAfterDisconnection);
            });
        }

        private async Task<IDisposable> CreateEventStoreSubscription(string streamId, IObserver<IEvent> obs, long? fromIncluding, bool rewindfAfterDisconnection)
        {
            if (fromIncluding != null || rewindfAfterDisconnection)
            {
                var settings = new CatchUpSubscriptionSettings(10000, 500, false, true, streamId);

                //SubscribeToStreamFrom lastChekcpoint is exclusive
                long? position = fromIncluding == null ? null : fromIncluding - 1;

                var catchUpSubscription = _eventStoreConnection.SubscribeToStreamFrom(streamId, position, settings, (eventStoreSubscription, resolvedEvent) =>
                {
                    var @event = DeserializeEvent(resolvedEvent.Event);

                    obs.OnNext(@event);

                }, (eventStoreCatchUpSubscription) => { }
                 , (subscription, reason, exception) =>
                 {
                     if(null != exception) obs.OnError(exception);
                     else
                     {
                         obs.OnError(new Exception($"{reason}"));
                     }
                 });

                return Disposable.Create(() =>
                {
                    catchUpSubscription.Stop(_eventStoreRepositoryConfiguration.ConnectionClosedTimeout);
                });

            }
            else
            {
                var eventStoreSubscription = await _eventStoreConnection.SubscribeToStreamAsync(streamId, true, (subscription, resolvedEvent) =>
                {
                    var @event = DeserializeEvent(resolvedEvent.Event);

                    obs.OnNext(@event);

                }, (subscription, reason, exception) =>
                {
                    if (null != exception) obs.OnError(exception);
                    else
                    {
                        obs.OnError(new Exception($"{reason}"));
                    }
                });

                return Disposable.Create(() =>
                {
                    eventStoreSubscription.Close();
                });
            }

        }

        private IEvent DeserializeEvent(RecordedEvent evt)
        {
            var @event = _eventStoreRepositoryConfiguration.Serializer.DeserializeObject(evt.Data, TypeUtil.Resolve(evt.EventType)) as IEvent;
            @event.Version = evt.EventNumber;
            return @event;
        }

        private IList<IList<EventData>> GetEventBatches(IEnumerable<EventData> events)
        {
            return events.Batch(_eventStoreRepositoryConfiguration.WritePageSize).Select(x => (IList<EventData>)x.ToList()).ToList();
        }

        private IDictionary<string, string> CreateCommitHeaders(KeyValuePair<string, string>[] extraHeaders)
        {
            var commitHeaders = GetCommitHeaders();

            foreach (var extraHeader in extraHeaders)
            {
                commitHeaders[extraHeader.Key] = extraHeader.Value;
            }

            return commitHeaders;
        }

        private EventData ToEventData(IEvent @event, IDictionary<string, string> headers)
        {
            var eventId = Guid.NewGuid();
            var data = _eventStoreRepositoryConfiguration.Serializer.SerializeObject(@event);

            var eventHeaders = new Dictionary<string, string>(headers)
            {
                {MetadataKeys.EventClrTypeHeader, @event.GetType().AssemblyQualifiedName}
            };

            var metadata = _eventStoreRepositoryConfiguration.Serializer.SerializeObject(eventHeaders);
            var typeName = @event.GetType().ToString();

            return new EventData(eventId, typeName, true, data, metadata);
        }

        private IDictionary<string, string> GetCommitHeaders()
        {
            var commitId = Guid.NewGuid();

            return new Dictionary<string, string>
            {
                {MetadataKeys.CommitIdHeader, commitId.ToString()},
                {MetadataKeys.UserIdentityHeader, Thread.CurrentPrincipal?.Identity?.Name},
                {MetadataKeys.ServerNameHeader, Environment.MachineName},
                {MetadataKeys.ServerClockHeader, DateTime.UtcNow.ToString("o")}
            };
        }

        public void Dispose()
        {
            _eventStoreConnection.Close();
            _connectionMonitor.Dispose();
        }


    }
}
