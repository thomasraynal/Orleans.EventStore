using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    public class EventStoreAdapterFactory : IQueueAdapterFactory
    {
        private readonly EventStoreRepositoryConfiguration _eventStoreRepositoryConfiguration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IStreamQueueMapper _streamQueueMapper;
        private readonly string _providerName;
        private readonly SimpleQueueAdapterCache _eventStoreQueueAdapterCache;

        public static EventStoreAdapterFactory Create(IServiceProvider services, string name)
        {
            var eventStoreRepositoryConfiguration = services.GetOptionsByName<EventStoreRepositoryConfiguration>(name);

            return ActivatorUtilities.CreateInstance<EventStoreAdapterFactory>(services, name, eventStoreRepositoryConfiguration);
        }

        public EventStoreAdapterFactory(string providerName, EventStoreRepositoryConfiguration eventStoreRepositoryConfiguration, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _providerName = providerName;

            _eventStoreRepositoryConfiguration = eventStoreRepositoryConfiguration;

            var options = new SimpleQueueCacheOptions()
            {
                CacheSize = 100
            };

            _eventStoreQueueAdapterCache = new SimpleQueueAdapterCache(options, _providerName, _loggerFactory);

            var hashRingStreamQueueMapperOptions = new HashRingStreamQueueMapperOptions() { TotalQueueCount = 1 };
            _streamQueueMapper = new HashRingBasedStreamQueueMapper(hashRingStreamQueueMapperOptions, _providerName);

        }

        public Task<IQueueAdapter> CreateAdapter()
        {
            var adapter = new EventStoreQueueAdapter(_providerName, _eventStoreRepositoryConfiguration, _loggerFactory);
            return Task.FromResult<IQueueAdapter>(adapter);
        }

        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
        {
            return Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler(false));
        }

        public IQueueAdapterCache GetQueueAdapterCache()
        {
            return _eventStoreQueueAdapterCache;
        }

        public IStreamQueueMapper GetStreamQueueMapper()
        {
            return _streamQueueMapper;
        }
    }
}
