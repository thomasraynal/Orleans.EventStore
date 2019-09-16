using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public interface IEventStore
    {
        IEventStoreConnection Connection { get; }
    }
}
