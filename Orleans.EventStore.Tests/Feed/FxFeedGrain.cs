﻿using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventStore.Tests
{

    [StorageProvider(ProviderName = "AsyncStreamHandlerStorage")]
    public class FxFeedGrain : Consumer<CcyPairChanged>, IFxFeedGrain<CcyPairChanged>
    {
        private readonly List<CcyPairChanged> _consumedEvents;
        private readonly Dictionary<string, ICcyPairGrain> _ccyPairs;

        public FxFeedGrain()
        {
            _consumedEvents = new List<CcyPairChanged>();
            _ccyPairs = new Dictionary<string, ICcyPairGrain>();
        }

        public Task Desactivate()
        {
            DeactivateOnIdle();

            return Task.CompletedTask;
        }

        public Task<IEnumerable<CcyPairChanged>> GetConsumedEvents()
        {
            return Task.FromResult(_consumedEvents.AsEnumerable());
        }

        public async override Task OnNextAsync(CcyPairChanged @event, StreamSequenceToken token)
        {
            _consumedEvents.Add(@event);

            if (!_ccyPairs.ContainsKey(@event.StreamId))
            {
                _ccyPairs[@event.StreamId] = GrainFactory.GetGrain<ICcyPairGrain>(@event.StreamId);

                Debug.WriteLine($"Create feed {@event.StreamId}");
            }

            await _ccyPairs[@event.StreamId].Tick(@event.Market, @event.Bid, @event.Ask);

        }

    }
}
