using System.Text.Json.Serialization;

namespace OwnID.Integrations.Gigya.Contracts.Accounts
{
    public class ResetPasswordResponse : BaseGigyaResponse
    {
        /// <summary>
        ///     The user ID of the user whose password was changed.
        ///     This field is only returned when the password is changed, not in calls that send an email or return a secret
        ///     question.
        /// </summary>
        [JsonPropertyName("UID")]
        public string UID { get; set; }
    }
}