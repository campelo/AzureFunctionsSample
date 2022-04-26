using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flavio.FunctionTest
{
    public class FunctionForTest
    {
        private readonly IConfiguration _configuration;
        private readonly IGraphClientService _graphClientService;

        public FunctionForTest(IConfiguration configuration, IGraphClientService graphClientService)
        {
            _configuration = configuration;
            _graphClientService = graphClientService;
        }

        [FunctionName("FunctionForTest")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger FunctionForTest processed a request.");

            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];
            string tenantId = _configuration["TenantId"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
            {
                return new BadRequestObjectResult(new { Error = "Please set ClientId, ClientSecret and TenantId. https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets" });
            }

            string maxString = null;
            if (req.Method == "GET")
                maxString = req.Query["max"];
            else if (req.Method == "POST")
            {
                string bodyString = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic body = Newtonsoft.Json.JsonConvert.DeserializeObject(bodyString);
                maxString = body?.max;
            }
            if (!int.TryParse(maxString, out int max))
                return new BadRequestObjectResult(new { Error = "Invalid or not found MAX param" });

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
            GraphServiceClient graphClient = await this._graphClientService.GetGraphClient(clientId, clientSecret, tenantId, scopes);

            var response = await graphClient.Users.Request().GetAsync();

            return new OkObjectResult(response.CurrentPage.Take(max)
                .Select(u => new { u.Id, u.DisplayName, u.GivenName, LastName = u.Surname }));
        }
    }
}
