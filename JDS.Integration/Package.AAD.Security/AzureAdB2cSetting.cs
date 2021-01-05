namespace Package.AAD.Security
{
    public class AzureAdB2CSetting
    {
        public string Tenant { get; set; }
        public string ClientId { get; set; }
        public string Instance { get; set; }
        public string PasswordPolicies { get; set; }
        public string ScopeWrite { get; set; }
        public string ScopeRead { get; set; }
        public string DefaultUserPassword { get; set; }

    }
}
