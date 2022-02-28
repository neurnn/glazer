using System.Threading;
using System.Threading.Tasks;

namespace Glazer.P2P.Hosting
{
    public interface IMessangerHost
    {
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
