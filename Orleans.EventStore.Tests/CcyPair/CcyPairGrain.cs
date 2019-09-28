using Orleans.Core;
using Orleans.EventSourcing;
using Orleans.EventSourcing.CustomStorage;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.EventStore.Tests
{

    [LogConsistencyProvider(ProviderName = "CcyPairEventStore")]
    [StorageProvider(ProviderName = "CcyPairStorage")]
    public class CcyPairGrain : EventStoreJournaledGrain<CcyPair,IEvent>, ICcyPairGrain
    {
        public CcyPairGrain(EventStoreRepositoryConfiguration eventStoreConfiguration) : base(eventStoreConfiguration)
        {
        }

        public async Task Activate()
        {
            RaiseEvent(new ActivateCcyPair(this.GetPrimaryKeyString()));
            await ConfirmEvents();
        }

        public async Task Desactivate()
        {
            RaiseEvent(new DesactivateCcyPair(this.GetPrimaryKeyString()));
            await ConfirmEvents();
        }

        public async Task<IEnumerable<IEvent>> GetAppliedEvents()
        {
            await ConfirmEvents();
            return await ReadStreamFromStorage();
        }

        public async Task<(double bid, double ask)> GetCurrentTick()
        {
            await ConfirmEvents();
            return (State.Bid, State.Ask);
        }

        public async Task<bool> GetIsActive()
        {
            await ConfirmEvents();
            return State.IsActive;
        }

        public async Task Tick(string market, double bid, double ask)
        {
         
            Debug.WriteLine($"bid:{bid} ask:{ask}");

            RaiseEvent(new ChangeCcyPairPrice(this.GetPrimaryKeyString(), market, bid, ask));

            await ConfirmEvents();
        }
    }
}
