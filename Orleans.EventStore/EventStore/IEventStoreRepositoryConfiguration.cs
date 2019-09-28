using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public interface IEventStoreRepositoryConfiguration : IEventStorePersistentSubscriptionConfiguration
    {
        int WritePageSize { get; }
        int ReadPageSize { get; }
        ISerializer Serializer { get; }
        string ConnectionString { get; }
        ConnectionSettings ConnectionSettings { get; }
    }
}
