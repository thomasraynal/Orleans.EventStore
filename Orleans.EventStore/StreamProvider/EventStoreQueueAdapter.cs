using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    public class EventStoreQueueAdapter : IQueueAdapter
    {

        private readonly ILoggerFactory _loggerFactory;

        public EventStoreQueueAdapter(string providerName, EventStoreRepositoryConfiguration eventStoreRepositoryConfiguration, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            Name = providerName;

            EventStore = EventStoreRepository.Create(eventStoreRepositoryConfiguration);
        }

        public string Name { get; }

        //todo: handle rewind
        //https://github.com/dotnet/orleans/tree/master/src/Azure/Orleans.Streaming.EventHubs
        public bool IsRewindable => false;

        public IEventStoreRepository EventStore { get; }

        public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

        public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
        {
            return EventStoreQueueAdapterReceiver.Create(EventStore, _loggerFactory, queueId, Name);
        }

        public async Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
        {

            if (!EventStore.IsStarted)
            {
                await EventStore.Connect(TimeSpan.FromSeconds(5));
            }

            //we handle versionning on EventStore
            await EventStore.SavePendingEvents(Name, ExpectedVersion.Any, events.Cast<IEvent>());

        }
    }
}
