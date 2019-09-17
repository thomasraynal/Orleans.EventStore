using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Streams;
using System.Linq;
using EventStore.ClientAPI;
using System.Net;
using EventStore.ClientAPI.SystemData;
using Orleans.CodeGeneration;

namespace Orleans.EventStore.Tests
{

    public interface IObserverGrain : IGrainWithGuidKey
    {
        Task Publish(ChangeCcyPairPrice changeCcyPairPrice);
        Task<IEnumerable<IEvent>> GetEvents();
    }

    public class ObserverGrain : Grain, IAsyncObserver<IEvent>, IObserverGrain
    {
        public List<IEvent> Events = new List<IEvent>();
        private IStreamProvider _streamProvider;
        private IAsyncStream<IEvent> _euroDolStream;

        public async override Task OnActivateAsync()
        {
            _streamProvider = base.GetStreamProvider("EUR/USD");

            _euroDolStream = _streamProvider.GetStream<IEvent>(Guid.Empty, "EUR/USD");

            var subscription = await _euroDolStream.SubscribeAsync(this);

        }

        public Task<IEnumerable<IEvent>> GetEvents()
        {
            return Task.FromResult(Events.AsEnumerable());
        }

        public async Task Publish(ChangeCcyPairPrice changeCcyPairPrice)
        {
            await _euroDolStream.OnNextAsync(changeCcyPairPrice);
        }

        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            return Task.CompletedTask;
        }

        public Task OnNextAsync(IEvent item, StreamSequenceToken token = null)
        {
            Events.Add(item);

            return Task.CompletedTask;
        }
    }

    [TestFixture]
    public class TestsEventStoreStreamProvider
    {
        private const string serviceId = "OrleansCcyPairs";
        private const string euroDolStream = "EUR/USD";
        private const string euroJpyStream = "EUR/JPY";
        private const string harmonyMarket = "Harmony";
        private const string fxConnectMarket = "fxConnect";
        private readonly List<string> groups = new List<string>() { harmonyMarket, fxConnectMarket };

        private EmbeddedEventStoreFixture _embeddedEventStore;

        [OneTimeSetUp]
        public async Task SetupFixture()
        {
            _embeddedEventStore = new EmbeddedEventStoreFixture();
            await _embeddedEventStore.Initialize();

            await Task.Delay(5000);
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _embeddedEventStore.Dispose();
        }

        private async Task<ISiloHost> CreateSilo(CancellationToken cancel)
        {

            var builder = new SiloHostBuilder()
                                .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("CcyPairStream")
                                .AddMemoryGrainStorage("CcyPairStorage")
                                .AddMemoryGrainStorage("AsyncStreamHandlerStorage")
                                .AddEventStorePersistentStream(euroDolStream)
                                .AddEventStorePersistentStream(euroJpyStream)
                                .AddMemoryGrainStorage("PubSubStore")
                                .UseLocalhostClustering()
                                .Configure<ClusterOptions>(options =>
                                {
                                    options.ClusterId = "dev";
                                    options.ServiceId = serviceId;
                                })
                                .ConfigureServices(services =>
                                {
                                    services.AddSingleton<IEventStoreStreamProviderConfiguration>(new EventStoreStreamProviderConfiguration(groups));
                                })
                               .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                               .ConfigureLogging(logging => logging.AddConsole());

            var host = builder.Build();
            await host.StartAsync(cancel);

            return host;

        }

        private async Task<IClusterClient> GetClient()
        {

            var client = new ClientBuilder()
                                        .UseLocalhostClustering()
                                        .AddEventStorePersistentStream(euroDolStream)
                                        .AddEventStorePersistentStream(euroJpyStream)
                                        .Configure<ClusterOptions>(options =>
                                        {
                                            options.ClusterId = "dev";
                                            options.ServiceId = serviceId;
                                        })
                                        .ConfigureLogging(logging => logging.AddConsole())
                                          .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                                        .Build();

            await client.Connect();
            return client;
        }

        [Test]
        public async Task ShouldWriteAndReadFromStream()
        {
            var cancel = new CancellationTokenSource();

            var silo = await CreateSilo(cancel.Token);
            var client = await GetClient();

            var observer2 = client.GetGrain<IObserverGrain>(Guid.NewGuid());
            var observer1 = client.GetGrain<IObserverGrain>(Guid.NewGuid());
            var observer3 = client.GetGrain<IObserverGrain>(Guid.NewGuid());
            var observer4 = client.GetGrain<IObserverGrain>(Guid.NewGuid());

            await observer1.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.32, 1.34));
            await observer1.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.33, 1.34));
            await observer1.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.31, 1.34));
            await observer1.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.35, 1.36));

            await Task.Delay(500);

            var observer1Events = await observer1.GetEvents();
            var observer2Events = await observer2.GetEvents();
            var observer3Events = await observer3.GetEvents();
            var observer4Events = await observer4.GetEvents();

            Assert.AreEqual(4, observer1Events.Count());
            Assert.AreEqual(4, observer2Events.Count());
            Assert.AreEqual(4, observer3Events.Count());
            Assert.AreEqual(4, observer4Events.Count());

            await observer1.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.32, 1.34));
            await observer1.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.33, 1.34));
            await observer1.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.31, 1.34));
            await observer1.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.35, 1.36));
            await observer2.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.32, 1.34));
            await observer2.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.33, 1.34));
            await observer2.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.31, 1.34));
            await observer2.Publish(new ChangeCcyPairPrice("EUR/USD", "Harmony", 1.35, 1.36));

            await Task.Delay(500);

            var events = await observer1.GetEvents();
            observer1Events = await observer1.GetEvents();
            observer2Events = await observer2.GetEvents();

            Assert.AreEqual(4, events.Count());
            Assert.AreEqual(4, observer1Events.Count());
            Assert.AreEqual(4, observer2Events.Count());

            cancel.Cancel();
            await silo.StopAsync();
            client.Dispose();

        }
    }
}
