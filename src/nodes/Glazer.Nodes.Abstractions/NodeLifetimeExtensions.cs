using System.Threading.Tasks;

namespace Glazer.Nodes.Abstractions
{
    public static class NodeLifetimeExtensions
    {
        /// <summary>
        /// Wait for starting the node detail service.
        /// </summary>
        /// <param name="Lifetime"></param>
        /// <returns></returns>
        public static async Task WaitStartedAsync(this INodeLifetime Lifetime)
        {
            var Tcs = new TaskCompletionSource();
            using (Lifetime.Started.Register(Tcs.SetResult))
                await Tcs.Task;
        }

        /// <summary>
        /// Wait for stopping the node detail service.
        /// </summary>
        /// <param name="Lifetime"></param>
        /// <returns></returns>
        public static async Task WaitStoppingAsync(this INodeLifetime Lifetime)
        {
            var Tcs = new TaskCompletionSource();
            using (Lifetime.Stopping.Register(Tcs.SetResult))
                await Tcs.Task;
        }
    }
}
