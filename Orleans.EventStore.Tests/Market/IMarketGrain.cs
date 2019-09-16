using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventStore.Tests
{
    public interface IMarketGrain : IGrainWithStringKey, ICanConnect
    {
        Task Tick(string ccyPair, double bid, double ask);
    }
}
