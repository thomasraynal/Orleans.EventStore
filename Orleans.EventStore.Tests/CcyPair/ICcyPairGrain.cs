using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventStore.Tests
{
    public interface ICcyPairGrain : IGrainWithStringKey
    {
        Task Activate();
        Task Desactivate();
        Task Tick(string market, double bid, double ask);
        Task<bool> GetIsActive();
        Task<(double bid, double ask)> GetCurrentTick();
        Task<IEnumerable<IEvent>> GetAppliedEvents();
    }
}
