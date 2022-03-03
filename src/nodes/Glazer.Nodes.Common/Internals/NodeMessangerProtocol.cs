using Glazer.Nodes.Abstractions;
using Glazer.P2P.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Internals
{
    public abstract class NodeMessangerProtocol : IMessangerProtocol
    {
        private readonly string m_TypeName;

        /// <summary>
        /// Initialize a new <see cref="NodeMessangerProtocol"/> instance.
        /// </summary>
        /// <param name="TypeName"></param>
        public NodeMessangerProtocol(string TypeName) => m_TypeName = TypeName;

        /// <inheritdoc/>
        public bool Handle(IMessanger Messanger, Message Message)
        {
            var Type = Message.Type;

            if (!string.IsNullOrWhiteSpace(Type) &&
                Type.Equals(m_TypeName, StringComparison.OrdinalIgnoreCase))
            {
                Messanger.Emit(Message);
                _ = Task.Run(() =>
                {
                    OnMessageAsync(Messanger.Services, Message)
                        .ConfigureAwait(false).GetAwaiter()
                        .GetResult();
                });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Called to handle the received message.
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Message"></param>
        protected abstract Task OnMessageAsync(IServiceProvider Services, Message Message);
    }
}
