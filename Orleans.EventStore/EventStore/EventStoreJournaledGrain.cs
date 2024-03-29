﻿using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using Orleans;
using Orleans.Core;
using Orleans.EventSourcing;
using Orleans.EventSourcing.CustomStorage;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    public abstract class EventStoreJournaledGrain<TState, TEvent> : JournaledGrain<TState, TEvent>, ICustomStorageInterface<TState, TEvent>
        where TState : class, IAggregate, new()
        where TEvent : class, IEvent
    {

        private readonly EventStoreRepositoryConfiguration _eventStoreConfiguration;
        private EventStoreRepository _repository;

        protected EventStoreJournaledGrain(EventStoreRepositoryConfiguration eventStoreConfiguration)
        {
            _eventStoreConfiguration = eventStoreConfiguration;
        }

        public async override Task OnActivateAsync()
        {
            //todo: we may not need a connection per grain...
            _repository = EventStoreRepository.Create(_eventStoreConfiguration);
            await _repository.Connect(TimeSpan.FromSeconds(5));
        }

        public override Task OnDeactivateAsync()
        {
            _repository.Dispose();

            return Task.CompletedTask;
        }

        public async Task<bool> ApplyUpdatesToStorage(IReadOnlyList<TEvent> updates, int expectedversion)
        {
            try
            {
                await _repository.SavePendingEvents(this.GetPrimaryKeyString(), Version - 1, updates.Cast<IEvent>());
            }
            //https://dotnet.github.io/orleans/Documentation/grains/event_sourcing/log_consistency_providers.html
            catch (WrongExpectedVersionException ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public async Task<IEnumerable<IEvent>> ReadStreamFromStorage()
        {
            return await _repository.GetStream(this.GetPrimaryKeyString());
        }

        public async Task<KeyValuePair<int, TState>> ReadStateFromStorage()
        {
            var (version, state) = await _repository.GetAggregate<string, TState>(this.GetPrimaryKeyString());

            return KeyValuePair.Create(version, state);
        }
    }
}
