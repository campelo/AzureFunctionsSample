using AzFunctionsSample;
using AzFunctionsSample.Options;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var host = new HostBuilder()
    .ConfigureServices((builderContext, services) =>
    {

        services
            .AddSingleton<IGraphClientService, GraphClientService>()
            .Configure<ServiceBusOptions>(builderContext.Configuration.GetSection(ServiceBusOptions.OptionsName))
            .AddSingleton((provider) =>
            {
                ServiceBusOptions options = provider
                .GetRequiredService<IOptions<ServiceBusOptions>>()
                .Value;
                var client = new ServiceBusClient(options.ConnectionString);
                return client;
            });
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
