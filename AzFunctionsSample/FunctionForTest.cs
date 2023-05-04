using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace AzFunctionsSample
{
    public class FunctionForTest
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IGraphClientService _graphClientService;

        public FunctionForTest(ILoggerFactory loggerFactory, IConfiguration configuration, IGraphClientService graphClientService)
        {
            _logger = loggerFactory.CreateLogger<FunctionForTest>();
            _configuration = configuration;
            _graphClientService = graphClientService;
        }

        [Function(nameof(Run))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger FunctionForTest processed a request.");

            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];
            string tenantId = _configuration["TenantId"];

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
            GraphServiceClient graphClient = await this._graphClientService.GetGraphClient(clientId, clientSecret, tenantId, scopes);

            var response = await graphClient.Users.Request().GetAsync();

            var result = req.CreateResponse();
            result.StatusCode = System.Net.HttpStatusCode.OK;
            result.WriteString(JsonConvert.SerializeObject(response.CurrentPage.Take(max)
                .Select(u => new { u.Id, u.DisplayName, u.GivenName, LastName = u.Surname })));
            return result;
        }

        [Function(nameof(ShowSettings))]
        public async Task<HttpResponseData> ShowSettings([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var result = string.Join(",", _configuration.GetSection("TestArray").GetChildren().Select(x => x.Value));

            _logger.LogInformation(result);

            var resp = req.CreateResponse();
            resp.StatusCode = System.Net.HttpStatusCode.OK;
            resp.WriteString(result);
            return resp;
        }
    }
}
