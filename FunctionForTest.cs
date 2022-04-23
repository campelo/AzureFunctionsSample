using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Flavio.FunctionTest
{
  public class FunctionForTest
  {
    private readonly IConfiguration _configuration;

    public FunctionForTest(IConfiguration configuration)
    {
      _configuration = configuration;
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

      IConfidentialClientApplication msalClient = ConfidentialClientApplicationBuilder
          .Create(clientId)
          .WithAuthority(AadAuthorityAudience.AzureAdMyOrg, true)
          .WithTenantId(tenantId)
          .WithClientSecret(clientSecret)
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

      return new OkObjectResult(new
      {
        Result = response.CurrentPage.Take(max)
          .Select(u => new { u.Id, u.DisplayName, u.GivenName, LastName = u.Surname })
      });
    }
  }
}
