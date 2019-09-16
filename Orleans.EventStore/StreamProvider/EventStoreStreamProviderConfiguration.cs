using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public class EventStoreStreamProviderConfiguration : EventStoreRepositoryConfiguration, IEventStoreStreamProviderConfiguration
    {
        public EventStoreStreamProviderConfiguration()
        {
        }

        public EventStoreStreamProviderConfiguration(IList<string> groups)
        {
            Groups = groups;
        }

        public IList<string> Groups { get; private set; }
    }
}
