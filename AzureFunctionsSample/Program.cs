using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Flavio.FunctionTest
{
    //public class Program : FunctionsStartup
    //{
    //    public override void Configure(IFunctionsHostBuilder builder)
    //    {
    //        builder.Services.AddSingleton<IGraphClientService, GraphClientService>();
    //    }
    //}

    public static class Program
    {
        public static Task Main()
        {
            var host = new HostBuilder()
                .ConfigureServices((builderContext, services) =>
                {
                    services
                        .AddSingleton<IGraphClientService, GraphClientService>();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureOpenApi()
                .Build();
            return host.RunAsync();
        }

    }
}