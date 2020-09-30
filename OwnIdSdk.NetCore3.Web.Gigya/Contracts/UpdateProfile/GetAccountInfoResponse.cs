using System.Text.Json.Serialization;

namespace OwnIdSdk.NetCore3.Web.Gigya.Contracts.UpdateProfile
{
    public class GetAccountInfoResponse<TProfile> : BaseGigyaResponse where TProfile : class, IGigyaUserProfile
    {
        [JsonPropertyName("UID")]
        public string DID { get; set; }

        public AccountData Data { get; set; }

        public TProfile Profile { get; set; }
    }
}