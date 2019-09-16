using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventStore
{
    public interface ICanConnect
    {
        Task Connect(string provider);
        Task Disconnect();
    }
}
