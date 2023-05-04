using System.Threading.Tasks;
using Microsoft.Graph;

namespace AzFunctionsSample
{
    public interface IGraphClientService
    {
        Task<GraphServiceClient> GetGraphClient(string clientId, string clientSecret, string tenantId, string[] scopes);
    }
}