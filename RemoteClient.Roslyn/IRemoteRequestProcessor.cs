using System;
using System.Threading.Tasks;

namespace RemoteClient.Roslyn
{
    public interface IRemoteRequestProcessor : IDisposable
    {
        Task<T> GetResultAsync<T>(IRemoteRequest request);
        Task ExecuteAsync(IRemoteRequest request);
    }
}
