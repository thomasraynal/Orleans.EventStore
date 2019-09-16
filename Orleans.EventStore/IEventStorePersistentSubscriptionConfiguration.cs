using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public interface IEventStorePersistentSubscriptionConfiguration
    {
        UserCredentials UserCredentials { get; }
        int BufferSize { get; }
        bool AutoAck { get; }
        TimeSpan ConnectionClosedTimeout { get; }
    }
}
