using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Package.AAD.Security.Services
{
    public class GraphServiceClientProvider : IGraphServiceClientProvider
    {
        private readonly GraphApiSetting GraphApiSetting;
        
        public GraphServiceClientProvider(IOptions<GraphApiSetting> graphApiSetting)
        {
            this.GraphApiSetting = graphApiSetting.Value;
        }
  
        //Use this to create GraphServiceClient if calling Graph API on behalf of a user
        //https://github.com/microsoftgraph/msgraph-sdk-dotnet-auth
        //https://docs.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS
        public async Task<GraphServiceClient> GraphServiceClientWithDelegateAuthenticationProviderAsync()
        {
            // *Never* include client secrets in source code!
            var clientSecret = await GetClientSecretFromKeyVault(); // Or some other secure place.

            // The app registration should be configured to require access to permissions
            // sufficient for the Microsoft Graph API calls the app will be making, and
            // those permissions should be granted by a tenant administrator.
            var scopes = GraphApiSetting.Scopes.Split(",");

            // Configure the MSAL client as a confidential client
            var confidentialClient = ConfidentialClientApplicationBuilder
                .Create(GraphApiSetting.ClientId)
                .WithAuthority(string.Format(GraphApiSetting.Authority, GraphApiSetting.TenantId))
                .WithClientSecret(clientSecret)
                .Build();

            // Build the Microsoft Graph client. As the authentication provider, set an async lambda
            // which uses the MSAL client to obtain an app-only access token to Microsoft Graph,
            // and inserts this access token in the Authorization header of each API request. 
            GraphServiceClient graphServiceClient =
            new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {

                            // Retrieve an access token for Microsoft Graph (gets a fresh token if needed).
                            var authResult = await confidentialClient
                    .AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                            // Add the access token in the Authorization header of the API request.
                            requestMessage.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            })
            );

            // Make a Microsoft Graph API query
            return graphServiceClient;
        }

        //Use this to create GraphServiceClient if calling Graph API on behalf of a Application/Service
        //https://github.com/microsoftgraph/msgraph-sdk-dotnet-auth
        //https://docs.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS
        public async Task<GraphServiceClient> GraphServiceClientWithClientCredentialProviderAsync()
        {
            // *Never* include client secrets in source code!
            var clientSecret = await GetClientSecretFromKeyVault(); // Or some other secure place.

            // The app registration should be configured to require access to permissions
            // sufficient for the Microsoft Graph API calls the app will be making, and
            // those permissions should be granted by a tenant administrator.
            var scopes = GraphApiSetting.Scopes.Split(",");

            // Configure the MSAL client as a confidential client
            var confidentialClient = ConfidentialClientApplicationBuilder
                .Create(GraphApiSetting.ClientId)
                .WithAuthority(string.Format(GraphApiSetting.Authority, GraphApiSetting.TenantId))
                .WithClientSecret(clientSecret)
                .Build();
                       
            // Create an authentication provider.
            ClientCredentialProvider authenticationProvider = new ClientCredentialProvider(confidentialClient);
            GraphServiceClient graphServiceClient = new GraphServiceClient(authenticationProvider);

            // Make a Microsoft Graph API query
            return graphServiceClient;
        }

        private async Task<string> GetClientSecretFromKeyVault()
        {
            return GraphApiSetting.ClientSecret;
        }
    }
}
