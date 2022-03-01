using Glazer.P2P.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.P2P.Hosting
{
    public interface IMessangerHost
    {
        /// <summary>
        /// Messanger Instance.
        /// </summary>
        public IMessanger Messanger { get; }

        /// <summary>
        /// Start the P2P host asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task StartAsync(CancellationToken Token = default);

        /// <summary>
        /// Stop the P2P host asynchronously.
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}
