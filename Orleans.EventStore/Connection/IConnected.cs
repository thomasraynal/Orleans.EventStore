using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventStore
{
    public interface IConnected<T>
    {
        T Value { get; }
        bool IsConnected { get; }
    }
}
