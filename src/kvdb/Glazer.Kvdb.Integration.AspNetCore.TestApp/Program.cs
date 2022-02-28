using Glazer.Kvdb.Memory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Glazer.Kvdb.Integration.AspNetCore.TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(Http =>
                {
                    Http.ConfigureKvdbApiOptions(Options =>  // --> Add the KVDB API options.
                    {
                        Options.DisableHttpWrite = false;
                    });

                    Http.ConfigureServices(Services =>
                    {
                        Services.SetKvScheme(_ =>
                        {
                            return new MemoryKvScheme();
                        });

                        Services
                            .AddControllers()
                            .AddKvdbApiControllers(); // --> Add the KVDB API sets.

                        Services.AddSwaggerGen(c =>
                        {
                            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Glazer.Kvdb.Integration.AspNetCore.TestApp", Version = "v1" });
                        });
                    });

                    Http.Configure((Web, App) =>
                    {
                        var Env = Web.HostingEnvironment;
                        if (Env.IsDevelopment())
                        {
                            App.UseDeveloperExceptionPage();
                            App.UseSwagger();
                            App.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Glazer.Kvdb.Integration.AspNetCore.TestApp v1"));
                        }

                        App.UseRouting();

                        App.UseAuthorization();

                        App.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });

                })
                .Build()
                .Run();
        }
    }
}
