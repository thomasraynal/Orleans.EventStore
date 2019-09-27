using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    public class EventStoreQueueAdapter : IQueueAdapter
    {
        private readonly IEventStoreRepositoryConfiguration _eventStoreRepositoryConfiguration;
        private readonly ILoggerFactory _loggerFactory;
        
        public EventStoreQueueAdapter(string providerName,
            IEventStoreRepositoryConfiguration eventStoreRepositoryConfiguration,
            ILoggerFactory loggerFactory)
        {
            _eventStoreRepositoryConfiguration = eventStoreRepositoryConfiguration;
            _loggerFactory = loggerFactory;

            Name = providerName;

            EventStore = EventStoreRepository.Create(eventStoreRepositoryConfiguration);
        }

        public string Name { get; }

        public bool IsRewindable => true;

        public IEventStoreRepository EventStore { get; }

        public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

        public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
        {
            //todo: create a connection per queue?
            return EventStoreQueueAdapterReceiver.Create(EventStore, _loggerFactory, queueId, Name);
        }

        public async Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
        {

            if (!EventStore.IStarted)
            {
                await EventStore.Connect(TimeSpan.FromSeconds(5));
            }

            //we handle versioning on EventStore
            await EventStore.SavePendingEvents(Name, ExpectedVersion.Any, events.Cast<IEvent>());

        }
    }
}
