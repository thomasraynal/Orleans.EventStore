using EventStore.ClientAPI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.EventStore.Tests
{
    [TestFixture]
    public class TestsEventStore
    {
        private EmbeddedEventStoreFixture _embeddedEventStore;
        private CompositeDisposable _cleanup;

        [OneTimeSetUp]
        public async Task SetupFixture()
        {
            _embeddedEventStore = new EmbeddedEventStoreFixture();
            await _embeddedEventStore.Initialize();
            _cleanup = new CompositeDisposable();
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _cleanup.Dispose();
            await _embeddedEventStore.Dispose();
        }

        [Test]
        public async Task ShouldCreateAndResumeConnection()
        {

            var subject = Guid.NewGuid().ToString();

            var repository = EventStoreRepository.Create(new EventStoreRepositoryConfiguration());

            _cleanup.Add(repository);

            await repository.Connect(TimeSpan.FromSeconds(10));

            //wait for EventStore to setup user accounts
            await Task.Delay(500);

            var counter = 0;

            var subscription = repository.Observe(subject)
                                       .Subscribe(ev =>
                                        {
                                            counter++;
                                        });

            _cleanup.Add(subscription);

            await repository.SavePendingEvents(subject, -1, new[] { new CcyPairChanged("Harmony", subject, true, 1.32, 1.34) });

            await Task.Delay(200);

            await _embeddedEventStore.Dispose();

            _embeddedEventStore = new EmbeddedEventStoreFixture();
            await _embeddedEventStore.Initialize();

            await Wait.Until(() => repository.IsConnected, TimeSpan.FromSeconds(5));

            //wait for EventStore to setup user accounts
            await Task.Delay(500);

            await repository.SavePendingEvents(subject, -1, new[] { new CcyPairChanged("Harmony", subject, true, 1.32, 1.34) });

            await Task.Delay(200);

            Assert.AreEqual(2, counter);


        }

        [Test]
        public async Task ShouldSubscribeFromVersion()
        {
            var subject = Guid.NewGuid().ToString();
            var repository = EventStoreRepository.Create(new EventStoreRepositoryConfiguration());

            _cleanup.Add(repository);

            await repository.Connect(TimeSpan.FromSeconds(10));

            //wait for EventStore to setup user accounts
            await Task.Delay(500);

            await repository.SavePendingEvents(subject, -1, new[] { new CcyPairChanged("Harmony", subject, true, 1.32, 1.34) });
            await Task.Delay(200);

            await repository.SavePendingEvents(subject, 0, new[] { new CcyPairChanged("Harmony", subject, true, 1.32, 1.34) });
            await Task.Delay(200);

            repository.Dispose();

            repository = EventStoreRepository.Create(new EventStoreRepositoryConfiguration());
            await repository.Connect(TimeSpan.FromSeconds(10));

            //wait for EventStore to setup user accounts
            await Task.Delay(500);

            var counter = 0;
            var subscription = repository.Observe(subject, 1)
                           .Subscribe(ev =>
                           {
                               counter++;
                           });

            _cleanup.Add(subscription);

            await Task.Delay(200);

            Assert.AreEqual(1, counter);
        }

        [Test]
        public async Task ShouldCatchUpStream()
        {
            var subject = Guid.NewGuid().ToString();
            var repository = EventStoreRepository.Create(new EventStoreRepositoryConfiguration());
            _cleanup.Add(repository);

            await repository.Connect(TimeSpan.FromSeconds(10));

            //wait for EventStore to setup user accounts
            await Task.Delay(500);

            await repository.SavePendingEvents(subject, -1, new[] { new CcyPairChanged("Harmony", subject, true, 1.32, 1.34) });
            await Task.Delay(200);

            await repository.SavePendingEvents(subject, 0, new[] { new CcyPairChanged("Harmony", subject, true, 1.32, 1.34) });
            await Task.Delay(200);

            repository.Dispose();

            repository = EventStoreRepository.Create(new EventStoreRepositoryConfiguration());
            await repository.Connect(TimeSpan.FromSeconds(10));

            //wait for EventStore to setup user accounts
            await Task.Delay(500);

            var counter = 0;
            var subscription = repository.Observe(subject, rewindAfterDisconnection: true)
                           .Subscribe(ev =>
                           {
                               counter++;
                           });

            _cleanup.Add(subscription);

            await Task.Delay(200);

            Assert.AreEqual(2, counter);
        }

    }
}
