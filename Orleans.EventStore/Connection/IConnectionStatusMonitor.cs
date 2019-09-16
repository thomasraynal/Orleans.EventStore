using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Orleans.EventStore
{
    public interface IConnectionStatusMonitor : IDisposable
    {
        bool IsConnected { get; }
        Task Connect();
    }
}