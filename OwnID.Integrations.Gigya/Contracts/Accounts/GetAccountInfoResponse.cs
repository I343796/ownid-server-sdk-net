using System.Text.Json.Serialization;

namespace OwnID.Integrations.Gigya.Contracts.Accounts
{
    public class GetAccountInfoResponse<TProfile> : BaseGigyaResponse where TProfile : class, IGigyaUserProfile
    {
        [JsonPropertyName("UID")]
        public string DID { get; set; }

        public AccountData Data { get; set; }

        public TProfile Profile { get; set; }
    }

    public class AccountInfoResponse<TProfile>
    {
        [JsonPropertyName("UID")]
        public string DID { get; set; }

        public AccountData Data { get; set; }

        public TProfile Profile { get; set; }
    }
}