using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Extensions;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.AccountRecovery;
using OwnID.Extensibility.Json;
using OwnID.Extensibility.Providers;
using OwnID.Integrations.Firebase.Contracts;
using OwnID.Integrations.Firebase.Extensions;

namespace OwnID.Integrations.Firebase.Handlers
{
    public class FirebaseRecoveryHandler : IAccountRecoveryHandler
    {
        private readonly IFirebaseContext _firebaseContext;
        private readonly HttpClient _httpClient;
        private readonly IRandomPasswordProvider _randomPasswordProvider;

        public FirebaseRecoveryHandler(IFirebaseContext firebaseContext, IHttpClientFactory httpClientFactory,
            IRandomPasswordProvider randomPasswordProvider)
        {
            _firebaseContext = firebaseContext;
            _randomPasswordProvider = randomPasswordProvider;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<AccountRecoveryResult> RecoverAsync(string accountRecoveryPayload)
        {
            var payload = OwnIdSerializer.Deserialize<RecoveryPayload>(accountRecoveryPayload);

            if (string.IsNullOrEmpty(payload?.OobCode) || string.IsNullOrEmpty(payload.ApiKey))
                throw new ArgumentException("Recovery code api key cannot be null", nameof(accountRecoveryPayload));

            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:resetPassword?key={payload.ApiKey}";
            var httpResult = await _httpClient.PostAsJsonAsync(url,
                new {oobCode = payload.OobCode, newPassword = _randomPasswordProvider.Generate()});
            var result =
                await httpResult.Content.ReadFromJsonAsync<RecoverPasswordResponse>(
                    OwnIdSerializer.GetDefaultProperties());

            if (!string.IsNullOrEmpty(result?.Error?.Message))
            {
                if (result.Error.Message == "EXPIRED_OOB_CODE")
                    throw new OwnIdException(ErrorType.RecoveryTokenExpired);

                throw new Exception($"Firebase REST -> {result.Error.Code}: {result.Error.Message}");
            }

            var user = await _firebaseContext.Auth.GetUserByEmailAsync(result!.Email);

            return new AccountRecoveryResult
            {
                DID = user.Uid
            };
        }

        public async Task ReplaceWithNewConnectionAsync(string did, OwnIdConnection connection)
        {
            var connectionId = string.IsNullOrEmpty(connection.Fido2CredentialId)
                ? connection.PublicKey.ToSha256()
                : connection.Fido2CredentialId;

            var newDbConnection = _firebaseContext.Db.Collection(Constants.CollectionName)
                .Document(connectionId.ReplaceSpecPathSymbols());

            var oldConnections = await _firebaseContext.Db.Collection(Constants.CollectionName)
                .WhereEqualTo(Constants.UserIdFieldName, did).GetSnapshotAsync();

            await _firebaseContext.Db.RunTransactionAsync(transaction =>
            {
                foreach (var oldConnection in oldConnections) transaction.Delete(oldConnection.Reference);

                transaction.Create(newDbConnection, new
                {
                    pubKey = connection.PublicKey,
                    keyHsh = connection.PublicKey.ToSha256(),
                    recoveryId = connection.RecoveryToken,
                    recoveryEncData = connection.RecoveryData,
                    fido2CredentialId = connection.Fido2CredentialId,
                    fido2SignatureCounter = connection.Fido2SignatureCounter,
                    userId = did,
                    authType = connection.AuthType.ToString()
                });

                return Task.CompletedTask;
            });
        }
    }
}