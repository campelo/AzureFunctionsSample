using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace Flavio.FunctionTest
{
  public class GraphClientService : IGraphClientService
  {
    public async Task<GraphServiceClient> GetGraphClient(string clientId, string clientSecret, string tenantId, string[] scopes)
    {
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
      return graphClient;
    }
  }
}