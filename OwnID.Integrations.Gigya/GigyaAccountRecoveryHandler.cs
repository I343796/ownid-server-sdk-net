using System;
using System.Threading.Tasks;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.AccountRecovery;
using OwnID.Extensibility.Json;
using OwnID.Extensibility.Providers;
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
        private readonly IRandomPasswordProvider _randomPasswordProvider;

        public GigyaAccountRecoveryHandler(GigyaRestApiClient<TProfile> apiClient, IRandomPasswordProvider randomPasswordProvider)
        {
            _apiClient = apiClient;
            _randomPasswordProvider = randomPasswordProvider;
        }

        public async Task<AccountRecoveryResult> RecoverAsync(string accountRecoveryPayload)
        {
            var payload = OwnIdSerializer.Deserialize<GigyaAccountRecoveryPayload>(accountRecoveryPayload);
            var newPassword = _randomPasswordProvider.Generate();

            var resetPasswordResponse = await _apiClient.ResetPasswordAsync(payload.ResetToken, newPassword);

            if (resetPasswordResponse.ErrorCode != 0)
                throw new OwnIdException(ErrorType.RecoveryTokenExpired, resetPasswordResponse.ErrorDetails);

            return new AccountRecoveryResult
            {
                DID = resetPasswordResponse.UID
            };
        }

        public async Task ReplaceWithNewConnectionAsync(string did, OwnIdConnection connection)
        {
            var gigyaOwnIdConnection = new GigyaOwnIdConnection(connection);

            var responseMessage =
                await _apiClient.SetAccountInfoAsync<TProfile>(did, data: new AccountData(gigyaOwnIdConnection));

            if (responseMessage.ErrorCode != 0)
                throw new Exception($"Gigya.setAccountInfo error -> {responseMessage.GetFailureMessage()}");
        }
    }
}