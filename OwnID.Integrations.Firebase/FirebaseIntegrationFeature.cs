using System;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Json;
using OwnID.Integrations.Firebase.Contracts;
using OwnID.Web.Extensibility;

namespace OwnID.Integrations.Firebase
{
    public class FirebaseIntegrationFeature : IFeature
    {
        private readonly IFirebaseConfiguration _configuration = new FirebaseConfiguration();

        public void ApplyServices(IServiceCollection services)
        {
            services.TryAddSingleton(_configuration);
            services.TryAddSingleton<IFirebaseContext>(provider =>
            {
                var projectId = OwnIdSerializer.Deserialize<FirebaseServiceCredentials>(_configuration.CredentialsJson)
                    .ProjectId;

                var coreConfiguration = provider.GetRequiredService<IOwnIdCoreConfiguration>();
                var client = new FirestoreClientBuilder {JsonCredentials = _configuration.CredentialsJson}.Build();
                var firestoreDb = FirestoreDb.Create(projectId, client);
                
                var firebaseApp = FirebaseApp.GetInstance(coreConfiguration.DID) ??
                                  FirebaseApp.Create(new AppOptions
                                  {
                                      Credential = GoogleCredential.FromJson(_configuration.CredentialsJson),
                                      ProjectId = projectId
                                  }, coreConfiguration.DID);

                var firebaseAuth = FirebaseAuth.GetAuth(firebaseApp);

                return new FirebaseContext
                {
                    Auth = firebaseAuth,
                    App = firebaseApp,
                    Db = firestoreDb
                };
            });
        }

        public IFeature FillEmptyWithOptional()
        {
            return this;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_configuration?.CredentialsJson))
                throw new InvalidOperationException("Firebase configuration can not be null");
        }

        public FirebaseIntegrationFeature WithConfig(Action<IFirebaseConfiguration> configAction)
        {
            configAction(_configuration);
            return this;
        }
    }
}