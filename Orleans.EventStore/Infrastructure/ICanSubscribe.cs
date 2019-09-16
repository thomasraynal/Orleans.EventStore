using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    public interface ICanSubscribe
    {
        Task Subscribe(string subject);
        Task Unsubscribe(string subject);
    }
}
