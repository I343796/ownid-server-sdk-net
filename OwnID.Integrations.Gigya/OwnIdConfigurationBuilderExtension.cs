using Microsoft.Extensions.DependencyInjection;
using OwnID.Web.Extensibility;

namespace OwnID.Integrations.Gigya
{
    public static class OwnIdConfigurationBuilderExtension
    {
        /// <summary>
        ///     Enables GIGYA authorization process with <see cref="GigyaUserHandler{TProfile}" /> and provided profile model
        /// </summary>
        /// <param name="builder">builder</param>
        /// <param name="dataCenter">
        ///     Gigya data center servers (us1, eu1 and etc.) If you are not sure of your site's data center,
        ///     <see cref="https://developers.gigya.com/display/GD/Finding+Your+Data+Center">Finding Your Data Center</see>.
        /// </param>
        /// <param name="apiKey">Gigya API key</param>
        /// <param name="secret">Gigya Secret key</param>
        /// <param name="userKey">Gigya User key (optional)</param>
        /// <param name="loginType">Login result. Cookie session or JWT ID Token</param>
        /// <typeparam name="TProfile">User profile model</typeparam>
        public static void UseGigya<TProfile>(this IExtendableConfigurationBuilder builder, string dataCenter,
            string apiKey, string secret, string userKey = null, GigyaLoginType loginType = GigyaLoginType.Session)
            where TProfile : class, IGigyaUserProfile
        {
            builder.Services.AddHttpClient();
            var gigyaFeature = new GigyaIntegrationFeature();

            gigyaFeature.WithConfig<TProfile>(x =>
            {
                x.DataCenter = dataCenter;
                x.ApiKey = apiKey;
                x.SecretKey = secret;
                x.UserKey = userKey;
                x.LoginType = loginType;
            });

            builder.AddOrUpdateFeature(gigyaFeature);
            builder.UseUserHandlerWithCustomProfile<GigyaUserProfile, GigyaUserHandler<GigyaUserProfile>>();
            builder.UseAccountLinking<GigyaAccountLinkHandler<GigyaUserProfile>>();
            builder.UseAccountRecovery<GigyaAccountRecoveryHandler<GigyaUserProfile>>();
        }

        /// <inheritdoc cref="UseGigya{TProfile}" />
        /// <summary>
        ///     Enables GIGYA authorization process with <see cref="GigyaUserHandler{GigyaGigyaUserProfile}" /> and
        ///     <see cref="GigyaUserProfile" />
        /// </summary>
        public static void UseGigya(this IExtendableConfigurationBuilder builder, string dataCenter, string apiKey,
            string secret, string userKey = null, GigyaLoginType loginType = GigyaLoginType.Session)
        {
            UseGigya<GigyaUserProfile>(builder, dataCenter, apiKey, secret, userKey, loginType);
        }
    }
}