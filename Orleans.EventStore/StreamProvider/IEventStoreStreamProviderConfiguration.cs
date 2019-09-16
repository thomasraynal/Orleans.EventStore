using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public interface IEventStoreStreamProviderConfiguration : IEventStoreRepositoryConfiguration
    {
        IList<string> Groups { get; }
    }
}
