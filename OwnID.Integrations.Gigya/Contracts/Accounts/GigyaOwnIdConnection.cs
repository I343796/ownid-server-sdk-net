using System.Text.Json.Serialization;
using OwnID.Extensibility.Flow.Contracts;

namespace OwnID.Integrations.Gigya.Contracts.Accounts
{
    public class GigyaOwnIdConnection : OwnIdConnection
    {
        public GigyaOwnIdConnection()
        {
        }

        public GigyaOwnIdConnection(OwnIdConnection ownIdConnection)
        {
            PublicKey = ownIdConnection.PublicKey;
            RecoveryToken = ownIdConnection.RecoveryToken;
            RecoveryData = ownIdConnection.RecoveryData;
            Fido2CredentialId = ownIdConnection.Fido2CredentialId;
            Fido2SignatureCounter = ownIdConnection.Fido2SignatureCounter;
            AuthType = ownIdConnection.AuthType;
        }

        [JsonPropertyName("pubKey")]
        public new string PublicKey { get; set; }

        // hsh is correct, gigya can not find field hash
        [JsonPropertyName("keyHsh")]
        public string Hash { get; set; }

        /// <summary>
        ///     Recovery token
        /// </summary>
        [JsonPropertyName("recoveryId")]
        public new string RecoveryToken { get; set; }

        /// <summary>
        ///     Recovery data. Encrypted
        /// </summary>
        [JsonPropertyName("recoveryEncData")]
        public new string RecoveryData { get; set; }
    }
}