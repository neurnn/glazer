using System;
using System.Threading.Tasks;

namespace Glazer.Nodes.Abstractions
{
    public interface INodeEngineWorker
    {
        /// <summary>
        /// Invoke the delegate on the engine worker.
        /// </summary>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        ValueTask InvokeAsync(Func<Task> Delegate);
    }
}
