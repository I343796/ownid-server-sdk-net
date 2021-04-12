using OwnID.Extensibility.Configuration.Profile;
using OwnID.Integrations.Firebase.Handlers;
using OwnID.Web.Extensibility;

namespace OwnID.Integrations.Firebase
{
    public static class FirebaseBuilderExtensions
    {
        public static void UseFirebase(this IExtendableConfigurationBuilder builder, string credentialsJson)
        {
            var feature = new FirebaseIntegrationFeature();
            feature.WithConfig(configuration => { configuration.CredentialsJson = credentialsJson; });

            builder.AddOrUpdateFeature(feature);
            builder.UseUserHandlerWithCustomProfile<EmptyProfile, FirebaseUserHandler>();
            builder.UseAccountLinking<FirebaseLinkHandler>();
            builder.UseAccountRecovery<FirebaseRecoveryHandler>();
        }
    }
}