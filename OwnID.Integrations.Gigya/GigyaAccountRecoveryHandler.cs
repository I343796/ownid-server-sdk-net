using System;
using System.Linq;
using System.Threading.Tasks;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.AccountRecovery;
using OwnID.Extensibility.Json;
using OwnID.Integrations.Gigya.ApiClient;
using OwnID.Integrations.Gigya.Contracts.AccountRecovery;
using OwnID.Integrations.Gigya.Contracts.Accounts;

namespace OwnID.Integrations.Gigya
{
    /// <summary>
    ///     Gigya specific implementation of <see cref="IAccountRecoveryHandler" />
    /// </summary>
    public class GigyaAccountRecoveryHandler<TProfile> : IAccountRecoveryHandler
        where TProfile : class, IGigyaUserProfile
    {
        private readonly GigyaRestApiClient<TProfile> _apiClient;

        public GigyaAccountRecoveryHandler(GigyaRestApiClient<TProfile> apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<AccountRecoveryResult> RecoverAsync(string accountRecoveryPayload)
        {
            var payload = OwnIdSerializer.Deserialize<GigyaAccountRecoveryPayload>(accountRecoveryPayload);
            var newPassword = PasswordGenerator.Generate();

            var resetPasswordResponse = await _apiClient.ResetPasswordAsync(payload.ResetToken, newPassword);

            if (resetPasswordResponse.ErrorCode != 0)
                throw new OwnIdException(ErrorType.RecoveryTokenExpired, resetPasswordResponse.ErrorDetails);

            return new AccountRecoveryResult
            {
                DID = resetPasswordResponse.UID
            };
        }

        public async Task OnRecoverAsync(string did, OwnIdConnection connection)
        {
            var gigyaOwnIdConnection = new GigyaOwnIdConnection(connection);

            var responseMessage =
                await _apiClient.SetAccountInfoAsync<TProfile>(did, data: new AccountData(gigyaOwnIdConnection));

            if (responseMessage.ErrorCode != 0)
                throw new Exception($"Gigya.setAccountInfo error -> {responseMessage.GetFailureMessage()}");
        }

        public async Task RemoveConnectionsAsync(string publicKey)
        {
            var result = await _apiClient.SearchByPublicKey(publicKey);

            if (string.IsNullOrEmpty(result?.UID))
                return;

            var connectionToRemove = result.Data.OwnId.Connections.Single(c => c.PublicKey == publicKey);
            result.Data.OwnId.Connections.Remove(connectionToRemove);

            await _apiClient.SetAccountInfoAsync<TProfile>(result.UID, data: result.Data);
        }
    }
}