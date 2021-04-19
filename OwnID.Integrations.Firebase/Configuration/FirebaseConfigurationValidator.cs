using System;
using OwnID.Extensibility.Configuration.Validators;

namespace OwnID.Integrations.Firebase.Configuration
{
    public class FirebaseConfigurationValidator : IConfigurationValidator<IFirebaseConfiguration>
    {
        public void FillEmptyWithOptional(IFirebaseConfiguration configuration)
        {
        }

        public void Validate(IFirebaseConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration?.CredentialsJson))
                throw new InvalidOperationException("Firebase configuration can not be null");
        }
    }
}