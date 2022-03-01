using Glazer.P2P.Abstractions;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.P2P.Hosting.Impls
{
    public class MessangerHost : IMessangerHost
    {
        private IMessanger m_Messanger;
        private Func<IMessanger, Message, Func<Task>, Task> m_Application;

        private CancellationTokenSource m_Cts;
        private Task m_Task;

        /// <summary>
        /// Initialize a new <see cref="MessangerHost"/> instance.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Application"></param>
        public MessangerHost(IMessanger Messanger, Func<IMessanger, Message, Func<Task>, Task> Application)
        {
            m_Messanger = Messanger;
            m_Application = Application;
        }

        /// <inheritdoc/>
        public IMessanger Messanger => m_Messanger;

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken Token = default)
        {
            m_Cts = new CancellationTokenSource();
            m_Task = RunAsync(m_Cts.Token);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task StopAsync()
        {
            m_Cts?.Cancel();

            if (m_Task != null)
                await m_Task;

            m_Cts?.Dispose();
        }

        /// <summary>
        /// Run the <see cref="MessangerHost"/> instance.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken Token)
        {

            try
            {
                while (!Token.IsCancellationRequested)
                {
                    Message Message;

                    try { Message = await m_Messanger.WaitAsync(Token); }
                    catch { break; }

                    if (m_Application is null)
                        continue;

                    _ = m_Application(m_Messanger, Message, () => Task.CompletedTask);
                }
            }

            finally
            {
                await m_Messanger.DisposeAsync();
            }
        }
    }
}
