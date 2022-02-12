using Backrole.Core.Abstractions;
using Backrole.Core.Hosting;
using Glazer.Core.Helpers;
using Glazer.Core.IO;
using Glazer.Core.Models;
using Glazer.Core.Nodes.Internals.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals.Remotes
{
    using Redirectors = Dictionary<Guid, Action<object>>;
    internal class RemoteNodeReceiver : BackgroundService
    {
        private Socket m_Socket;
        private RemoteNode m_Node;
        private Redirectors m_Redirectors = new();

        /// <summary>
        /// Initialize a new <see cref="RemoteNodeReceiver"/> instance.
        /// </summary>
        /// <param name="Client"></param>
        public RemoteNodeReceiver(RemoteNode Node, TcpClient Client)
        {
            m_Node = Node;
            m_Socket = Client.Client;
        }

        /// <inheritdoc/>
        protected override async Task RunAsync(CancellationToken Token)
        {
            var Cts = m_Node.GetRequiredService<CancellationTokenSource>();
            var Mapper = m_Node.GetRequiredService<MessageMapper>();
            var Sender = m_Node.GetRequiredService<RemoteNodeSender>();
            var Injector = m_Node.GetRequiredService<IServiceInjector>();

            async Task DispatchRequest(RemoteNodeSender Sender, Guid Guid, object Request)
                => await Sender.ReplyAsync(Guid, await m_Node.OnRequest(Request));

            while (!Token.IsCancellationRequested)
            {
                var Reader = await m_Socket.ReceiveChunkedAsync(Token);
                if (Reader is null)
                {
                    Cts.Cancel(); // -> break the RemoteNode instance's waiter.
                    break;
                }

                try
                {
                    using (Reader)
                    {
                        var Guid = Reader.ReadGuid();
                        var Opcode = Reader.ReadByte();
                        var Message = Reader.ReadByte() == 0
                            ? null : Mapper.Decode(Reader, Injector);

                        switch (Opcode) // --> Opcode
                        {
                            case 0x00: continue;
                            case 0x01: /* Request. */

                                ThreadPool.QueueUserWorkItem(_ =>
                                {
                                    DispatchRequest(Sender, Guid, Message)
                                        .GetAwaiter().GetResult();
                                });
                                break;

                            case 0x02: /* Response. */
                                Sender.DispatchReply(Guid, Message);
                                break;
                        }
                    }
                }
                catch
                {
                    Cts.Cancel();
                    break;
                }
            }
        }
    }
}
