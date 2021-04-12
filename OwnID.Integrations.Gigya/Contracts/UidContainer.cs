using System.Text.Json.Serialization;
using OwnID.Integrations.Gigya.Contracts.Accounts;

namespace OwnID.Integrations.Gigya.Contracts
{
    public class UidContainer
    {
        [JsonPropertyName("UID")]
        public string UID { get; set; }

        public AccountData Data { get; set; }
    }
}