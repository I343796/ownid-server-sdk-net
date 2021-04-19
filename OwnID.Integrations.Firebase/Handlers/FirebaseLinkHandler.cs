using System;
using System.Threading.Tasks;
using OwnID.Extensibility.Extensions;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Flow.Contracts.Link;
using OwnID.Extensibility.Json;
using OwnID.Integrations.Firebase.Contracts;
using OwnID.Integrations.Firebase.Extensions;

namespace OwnID.Integrations.Firebase.Handlers
{
    public class FirebaseLinkHandler : IAccountLinkHandler
    {
        private readonly IFirebaseContext _firebaseContext;

        public FirebaseLinkHandler(IFirebaseContext firebaseContext)
        {
            _firebaseContext = firebaseContext;
        }
        
        public async Task<LinkState> GetCurrentUserLinkStateAsync(string payload)
        {
            var jwt = OwnIdSerializer.Deserialize<JwtContainer>(payload)?.Jwt;

            if (string.IsNullOrEmpty(jwt))
                throw new Exception("No JWT was found in HttpRequest");
            
            var decodedToken = await _firebaseContext.Auth.VerifyIdTokenAsync(jwt);
            var did = decodedToken.Uid;
            
            var connections = await _firebaseContext.Db.Collection(Constants.CollectionName)
                .WhereEqualTo(Constants.UserIdFieldName, did).GetSnapshotAsync();

            return new LinkState(did, (uint) connections.Count);
        }

        public async Task OnLinkAsync(string did, OwnIdConnection connection)
        {
            var connectionId = string.IsNullOrEmpty(connection.Fido2CredentialId)
                ? connection.PublicKey.ToSha256().ReplaceSpecPathSymbols()
                : connection.Fido2CredentialId;
            
            await _firebaseContext.Db.Collection(Constants.CollectionName).Document(connectionId).CreateAsync(new
            {
                pubKey = connection.PublicKey,
                keyHsh = connection.PublicKey.ToSha256(),
                recoveryId = connection.RecoveryToken,
                recoveryEncData = connection.RecoveryData,
                fido2CredentialId = connection.Fido2CredentialId,
                fido2SignatureCounter = connection.Fido2SignatureCounter,
                userId = did,
                authType = connection.AuthType
            });
        }
    }
}