using AzFunctionsSample;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureServices((builderContext, services) =>
    {
        services
            .AddSingleton<IGraphClientService, GraphClientService>()
            .AddSingleton<ServiceBusClient>(new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnectionString")));
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
