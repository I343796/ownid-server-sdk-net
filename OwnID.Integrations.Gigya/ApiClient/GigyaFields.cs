using System;

namespace OwnID.Integrations.Gigya.ApiClient
{
    /// <summary>
    ///     Gigya fields available for querying
    /// </summary>
    [Flags]
    public enum GigyaFields
    {
        [GigyaFields("UID")]
        UID = 1,
        [GigyaFields("profile")]
        Profile = 2,
        [GigyaFields("profile.email")]
        ProfileEmail = 4,
        [GigyaFields("data.ownId.settings")]
        Settings = 8,
        [GigyaFields("data.ownId.connections")]
        Connections = 16,
        [GigyaFields("data.ownId.connections.pubKey")]
        ConnectionPublicKeys = 32,
        [GigyaFields("data.ownId.connections.keyHsh")]
        ConnectionPublicKeysHash = 64,
        [GigyaFields("data.ownId.connections.fido2CredentialId")]
        ConnectionFido2CredentialId = 128,
        [GigyaFields("data.ownId.connections.recoveryId")]
        ConnectionRecoveryId = 256
    }
}