using Backrole.Core.Abstractions;
using Backrole.Core.Builders;
using Glazer.Storages.Server;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Glazer.Storages.Daemon
{
    class Program
    {

        public static async Task Main(string[] Arguments)
        {
            await new HostBuilder()
                .ConfigureBlobStorageServerContainer(Server =>
                {
                    var Current = Path.Combine(Path.GetDirectoryName(
                        Assembly.GetEntryAssembly().Location), "data");

                    if (!Directory.Exists(Current))
                         Directory.CreateDirectory(Current);

                    Server.ConfigureServices(Services =>
                    {
                        Services.AddStorage(null, _ => new FileSystemBlobStorage(new DirectoryInfo(Current)));
                    });

                })
                .Build()
                .RunAsync();
        }
    }
}
