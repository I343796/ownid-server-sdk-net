using OwnID.Cryptography;
using OwnID.Integrations.IAS.Handlers;
using OwnID.Web.Extensibility;
using OwnID.Extensibility.Configuration.Profile;
using System.IO;

namespace OwnID.Integrations.IAS
{
    public static class OwnIdConfigurationBuilderExtension
    {
        public static void UseIAS(this IExtendableConfigurationBuilder builder, string public_key, string private_key)
        {
            // builder.Services.AddHttpClient();
            var iasFeature = new IASIntegrationFeature();

            iasFeature.WithConfig(x =>
            {
                x.jwtSigningCredentials = RsaHelper.LoadKeys(new StringReader(public_key), new StringReader(private_key));
            });

            builder.AddOrUpdateFeature(iasFeature);
            builder.UseUserHandlerWithCustomProfile<EmptyProfile, IASUserHandler>();
            builder.UseAccountLinking<IASAccountLinkHandler>();
            builder.UseAccountRecovery<IASAccountRecoveryHandler>();
        }
    }
}
