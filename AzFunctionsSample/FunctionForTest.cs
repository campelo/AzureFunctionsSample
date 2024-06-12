using AzFunctionsSample.Options;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace AzFunctionsSample;

public class FunctionForTest(
    ILoggerFactory loggerFactory,
    IConfiguration configuration,
    IGraphClientService graphClientService,
    ServiceBusClient serviceBusClient,
    IOptions<ServiceBusOptions> serviceBusOptions)
{
    private readonly ILogger log = loggerFactory.CreateLogger<FunctionForTest>();
    private readonly ServiceBusOptions busOptions = serviceBusOptions.Value;

    [Function(nameof(Run))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        log.LogInformation("C# HTTP trigger FunctionForTest processed a request.");

        string clientId = configuration["ClientId"];
        string clientSecret = configuration["ClientSecret"];
        string tenantId = configuration["TenantId"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
        {
            var resp = req.CreateResponse();
            resp.StatusCode = System.Net.HttpStatusCode.BadRequest;
            resp.WriteString("Please set ClientId, ClientSecret and TenantId. https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets");
            return resp;
        }

        string maxString = null;
        if (req.Method == "GET")
            maxString = "10";
        else if (req.Method == "POST")
        {
            string bodyString = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic body = JsonConvert.DeserializeObject(bodyString);
            maxString = body?.max;
        }
        if (!int.TryParse(maxString, out int max))
        {
            var resp = req.CreateResponse();
            resp.StatusCode = System.Net.HttpStatusCode.BadRequest;
            resp.WriteString("Invalid or not found MAX param");
            return resp;
        }

        string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
        GraphServiceClient graphClient = await graphClientService.GetGraphClient(clientId, clientSecret, tenantId, scopes);

        var response = await graphClient.Users.GetAsync();

        var result = req.CreateResponse();
        result.StatusCode = System.Net.HttpStatusCode.OK;
        result.WriteString(JsonConvert.SerializeObject(response.Value.Take(max)
            .Select(u => new { u.Id, u.DisplayName, u.GivenName, LastName = u.Surname })));
        return result;
    }

    [Function(nameof(ShowSettings))]
    public async Task<HttpResponseData> ShowSettings([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var result = string.Join(",", configuration.GetSection("TestArray").GetChildren().Select(x => x.Value));

        log.LogInformation(result);

        var resp = req.CreateResponse();
        resp.StatusCode = System.Net.HttpStatusCode.OK;
        resp.WriteString(result);
        return resp;
    }

    private class MyMessage
    {
        public string Message { get; set; }
        public int Value { get; set; }
    }

    [Function(nameof(CreateServiceBusMessage))]
    public async Task<HttpResponseData> CreateServiceBusMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "CreateServiceBusMessage")]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("CreateServiceBusMessage");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        MyMessage message = JsonConvert.DeserializeObject<MyMessage>(requestBody);

        if (message == null || string.IsNullOrEmpty(message.Message))
        {
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            badResponse.WriteString("Invalid message");
            return badResponse;
        }

        ServiceBusSender sender = serviceBusClient.CreateSender(busOptions.Queue.Normal);

        try
        {
            await sender.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(message)));
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.WriteString("Message sent to Service Bus");
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error sending message: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            errorResponse.WriteString("Failed to send message");
            return errorResponse;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    [Function("NormalFlow")]
    public async Task NormalFlow(
        [ServiceBusTrigger("%ServiceBus:Queue:Normal%", Connection = "ServiceBus:ConnectionString")]
        string myQueueItem, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("NormalFlow");
        logger.LogInformation("Processing message from flow.normal queue: {myQueueItem}", myQueueItem);

        // Process message logic
        // For demonstration, we'll move the message to rejected queue if value is less than 10
        MyMessage message = JsonConvert.DeserializeObject<MyMessage>(myQueueItem);
        if (message != null && message.Value < 10)
        {
            ServiceBusSender sender = serviceBusClient.CreateSender(busOptions.Queue.Rejected);
            try
            {
                await sender.SendMessageAsync(new ServiceBusMessage(myQueueItem));
            }
            catch (Exception ex)
            {
                logger.LogError("Error moving message to flow.rejected queue: {message}", ex.Message);
            }
            finally
            {
                await sender.DisposeAsync();
            }
        }
    }

    [Function("RejectedFlow")]
    public async Task RejectedFlow(
        [ServiceBusTrigger("%ServiceBus:Queue:Rejected%", Connection = "ServiceBus:ConnectionString")]
        string myQueueItem, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("RejectedFlow");
        logger.LogInformation("Processing rejected message: {myQueueItem}", myQueueItem);
    }
}
