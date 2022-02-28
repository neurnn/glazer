using Glazer.P2P.Abstractions;
using Glazer.P2P.Protocols;
using Glazer.P2P.Tcp;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.P2P.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var Messanger = TcpMessanger.RandomPort(IPAddress.Any);
            Console.WriteLine($"Started on {Messanger.Endpoint}");

            Messanger.OnPeerEntered += X => Console.WriteLine($"Entered: {X}");
            Messanger.OnPeerLeaved += X => Console.WriteLine($"Leaved: {X}");

            var Task = ReceiveAsync(Messanger);
            while(true)
            {
                var Cmd = Console.ReadLine();
                if (Cmd.StartsWith("conn"))
                {
                    try { Messanger.Contact(IPEndPoint.Parse(Cmd.Substring(4).Trim())); }
                    catch(Exception e)
                    {
                        Console.WriteLine($"failed to add contact: {e}");
                    }
                }

                else if(Cmd.StartsWith("emit"))
                {
                    Messanger.Emit(new Message()
                    {
                        Type = "emit",
                        Data = Encoding.UTF8.GetBytes(Cmd.Substring(4).Trim())
                    });
                }

                else if (Cmd.Equals("invite"))
                {
                    Messanger.InvitePeers();
                }
            }
        }

        private static async Task ReceiveAsync(TcpMessanger Messanger)
        {
            while(true)
            {
                var Msg = await Messanger.WaitAsync();
                if (Msg.Type == "emit")
                {
                    Console.WriteLine($"Sender: {Msg.Sender.PublicKey}");
                    Console.WriteLine(Encoding.UTF8.GetString(Msg.Data));
                }
            }
        }
    }
}
