using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.P2P.Abstractions
{
    public interface IMessangerProtocol
    {
        /// <summary>
        /// Handle the received message once.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        bool Handle(IMessanger Messanger, Message Message);
    }
}
