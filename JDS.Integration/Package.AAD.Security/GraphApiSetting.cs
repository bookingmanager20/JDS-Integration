namespace Package.AAD.Security
{
    public class GraphApiSetting
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Authority { get; set; }
        public string Scopes { get; set; }
        public string GraphApiUrl { get; set; }
        public string B2CExtensionAppClientId { get; set; }
    }
}
