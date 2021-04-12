using System.Text.Json.Serialization;

namespace OwnID.Integrations.Gigya.Contracts.Jwt
{
    public class GetJwtResponse : BaseGigyaResponse
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }
    }
}