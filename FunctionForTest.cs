using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Flavio.FunctionTest
{
    public static class FunctionForTest
    {
        [FunctionName("FunctionForTest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger FunctionForTest processed a request.");

            if (!int.TryParse(req.Query["max"], out int max))
                return new BadRequestObjectResult(new {Error = "Invalid or no valid MAX param"});

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            IConfidentialClientApplication msalClient = ConfidentialClientApplicationBuilder
                .Create(Environment.GetEnvironmentVariable("ClientId"))
                .WithAuthority(AadAuthorityAudience.AzureAdMyOrg, true)
                .WithTenantId(Environment.GetEnvironmentVariable("TenantId"))
                .WithClientSecret(Environment.GetEnvironmentVariable("ClientSecret"))
                .Build();

            string token = null;
            AuthenticationResult cacheResult = await msalClient.AcquireTokenForClient(scopes).ExecuteAsync();
            token = cacheResult.AccessToken;

            GraphServiceClient graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(
                async (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    await Task.FromResult(0);
                }));

            var response = await graphClient.Users.Request().GetAsync();

            return new OkObjectResult(new { Result = response.CurrentPage.Take(max)
                .Select(u => new { u.Id, u.DisplayName, u.GivenName, LastName = u.Surname }) });
        }
    }
}
