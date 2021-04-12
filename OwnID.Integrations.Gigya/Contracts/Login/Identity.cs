using System.Text.Json.Serialization;

namespace OwnID.Integrations.Gigya.Contracts.Login
{
    public class Identity
    {
        public string Provider { get; set; }

        [JsonPropertyName("providerUID")]
        public string ProviderUID { get; set; }

        public string Email { get; set; }

        public string NickName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string LastUpdated { get; set; }

        public bool AllowsLogin { get; set; }

        public bool IsLoginIdentity { get; set; }

        public bool IsExpiredSession { get; set; }
    }
}