using AzFunctionsSample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureServices((builderContext, services) =>
    {
        services
            .AddSingleton<IGraphClientService, GraphClientService>();
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
