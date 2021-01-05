using Microsoft.Graph;
using System.Threading.Tasks;

namespace Package.AAD.Security.Services
{
    public interface IGraphServiceClientProvider
    {
        Task<GraphServiceClient> GraphServiceClientWithDelegateAuthenticationProviderAsync();
        Task<GraphServiceClient> GraphServiceClientWithClientCredentialProviderAsync();
    }
}
