using System;
using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using OwnID.Extensibility.Configuration.Profile;
using OwnID.Extensibility.Extensions;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Fido2;
using OwnID.Extensibility.Flow.Contracts.Internal;
using OwnID.Integrations.Firebase.Contracts;
using OwnID.Integrations.Firebase.Extensions;

namespace OwnID.Integrations.Firebase.Handlers
{
    public class FirebaseUserHandler : IUserHandler<EmptyProfile>
    {
        private readonly IFirebaseContext _firebaseContext;

        public FirebaseUserHandler(IFirebaseContext firebaseContext)
        {
            _firebaseContext = firebaseContext;
        }

        public Task CreateProfileAsync(IUserProfileFormContext<EmptyProfile> context, string recoveryToken = null,
            string recoveryData = null)
        {
            return Task.FromException(new NotSupportedException());
        }

        public Task UpdateProfileAsync(IUserProfileFormContext<EmptyProfile> context)
        {
            return Task.FromException(new NotSupportedException());
        }

        public Task<AuthResult<object>> RegisterPartialAsync(string did, OwnIdConnection ownIdConnection)
        {
            return Task.FromException<AuthResult<object>>(new NotSupportedException());
        }

        public async Task<AuthResult<object>> OnSuccessLoginAsync(string did, string publicKey)
        {
            return await OnSuccessLoginByPublicKeyAsync(publicKey);
        }

        public async Task<AuthResult<object>> OnSuccessLoginByPublicKeyAsync(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentNullException(nameof(publicKey));

            return await AuthorizeByConnection(publicKey.ToSha256());
        }

        public async Task<AuthResult<object>> OnSuccessLoginByFido2Async(string fido2CredentialId,
            uint fido2SignCounter)
        {
            if (string.IsNullOrWhiteSpace(fido2CredentialId))
                throw new ArgumentNullException(nameof(fido2CredentialId));

            return await AuthorizeByConnection(fido2CredentialId);
        }

        public async Task<IdentitiesCheckResult> CheckUserIdentitiesAsync(string did, string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentNullException(nameof(publicKey));

            var connection = await _firebaseContext.Db.Collection(Constants.CollectionName)
                .Document(publicKey.ToSha256().ReplaceSpecPathSymbols())
                .GetSnapshotAsync();

            if (!connection.Exists)
                return IdentitiesCheckResult.UserNotFound;

            var connectionDto = connection.ConvertTo<Connection>();

            if (connectionDto.PublicKey != publicKey)
                return IdentitiesCheckResult.WrongPublicKey;

            return IdentitiesCheckResult.UserExists;
        }

        public async Task<bool> IsUserExists(string publicKey)
        {
            var connection = await _firebaseContext.Db.Collection(Constants.CollectionName)
                .Document(publicKey.ToSha256().ReplaceSpecPathSymbols())
                .GetSnapshotAsync();

            return connection.Exists;
        }

        public async Task<bool> IsFido2UserExists(string fido2CredentialId)
        {
            var connection = await _firebaseContext.Db.Collection(Constants.CollectionName)
                .Document(fido2CredentialId.ReplaceSpecPathSymbols())
                .GetSnapshotAsync();

            return connection.Exists;
        }

        public async Task<Fido2Info> FindFido2Info(string fido2CredentialId)
        {
            var connection = await _firebaseContext.Db.Collection(Constants.CollectionName)
                .Document(fido2CredentialId.ReplaceSpecPathSymbols())
                .GetSnapshotAsync();

            if (!connection.Exists)
                throw new Exception("User was not found");

            var connectionDto = connection.ConvertTo<Connection>();

            return new Fido2Info
            {
                CredentialId = connectionDto.Fido2CredentialId,
                PublicKey = connectionDto.PublicKey,
                SignatureCounter = uint.Parse(connectionDto.Fido2SignatureCounter),
                UserId = connectionDto.UserId
            };
        }

        public async Task<ConnectionRecoveryResult<EmptyProfile>> GetConnectionRecoveryDataAsync(string recoveryToken,
            bool includingProfile = false)
        {
            if (string.IsNullOrWhiteSpace(recoveryToken))
                throw new ArgumentNullException(nameof(recoveryToken));

            var connections = await _firebaseContext.Db.Collection(Constants.CollectionName)
                .WhereEqualTo("recoveryId", recoveryToken).GetSnapshotAsync();

            if (connections.Count == 0)
                throw new Exception("User not found");

            var connection = connections.First().ConvertTo<Connection>();
            return new ConnectionRecoveryResult<EmptyProfile>
            {
                PublicKey = connection.PublicKey,
                RecoveryData = connection.RecoveryData,
                DID = connection.UserId
            };
        }

        public Task<string> GetUserIdByEmail(string email)
        {
            return Task.FromException<string>(new NotSupportedException());
        }

        public async Task<UserSettings> GetUserSettingsAsync(string publicKey)
        {
            var connection = await _firebaseContext.Db.Collection(Constants.CollectionName)
                .Document(publicKey.ToSha256().ReplaceSpecPathSymbols())
                .GetSnapshotAsync();
            
            if (!connection.Exists)
                throw new Exception("User was not found");

            var userId = connection.GetValue<string>("userId");
            
            var userSettings = await _firebaseContext.Db.Collection(Constants.CollectionName)
                .Document(userId)
                .GetSnapshotAsync();

            var result = new UserSettings();

            if (userSettings.Exists)
                result.EnforceTFA = userSettings.GetValue<bool>("enforceTfa");
            
            return result;
        }

        public async Task UpgradeConnectionAsync(string publicKey, OwnIdConnection newConnection)
        {
            var oldConnectionQuery = _firebaseContext.Db.Collection(Constants.CollectionName)
                .Document(publicKey.ToSha256());
            var oldConnectionSnapshot = await oldConnectionQuery.GetSnapshotAsync();

            if (!oldConnectionSnapshot.Exists)
                throw new Exception("User not found");

            await _firebaseContext.Db.RunTransactionAsync(async transaction =>
            {
                transaction.Delete(oldConnectionQuery);
                var connectionId = string.IsNullOrEmpty(newConnection.Fido2CredentialId)
                    ? newConnection.PublicKey.ToSha256()
                    : newConnection.Fido2CredentialId;

                var newDbConnection = _firebaseContext.Db.Collection(Constants.CollectionName).Document(connectionId);
                transaction.Create(newDbConnection, new
                {
                    pubKey = newConnection.PublicKey,
                    keyHsh = newConnection.PublicKey.ToSha256(),
                    recoveryId = newConnection.RecoveryToken,
                    recoveryEncData = newConnection.RecoveryData,
                    fido2CredentialId = newConnection.Fido2CredentialId,
                    fido2SignatureCounter = newConnection.Fido2SignatureCounter,
                    userId = oldConnectionSnapshot.GetValue<string>("userId")
                });
            });
        }

        public Task<string> GetUserNameAsync(string did)
        {
            return Task.FromException<string>(new NotSupportedException());
        }

        private async Task<AuthResult<object>> AuthorizeByConnection(string connectionId)
        {
            var connection = await _firebaseContext.Db.Collection("ownid")
                .Document(connectionId.ReplaceSpecPathSymbols())
                .GetSnapshotAsync();

            if (!connection.Exists)
                return new AuthResult<object>("User was not found");

            var customToken = await CreateCustomTokenAsync(connection.GetValue<string>("userId"));

            return new AuthResult<object>(new
            {
                idToken = customToken
            });
        }

        private async Task<string> CreateCustomTokenAsync(string id)
        {
            return await FirebaseAuth.GetAuth(_firebaseContext.App).CreateCustomTokenAsync(id);
        }
    }
}