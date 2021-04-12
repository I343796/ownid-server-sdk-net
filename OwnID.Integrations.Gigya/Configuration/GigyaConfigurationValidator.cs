using System;
using OwnID.Extensibility.Configuration.Validators;

namespace OwnID.Integrations.Gigya.Configuration
{
    public class GigyaConfigurationValidator : IConfigurationValidator<IGigyaConfiguration>
    {
        public void FillEmptyWithOptional(IGigyaConfiguration configuration)
        {
        }

        public void Validate(IGigyaConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.ApiKey) ||
                string.IsNullOrWhiteSpace(configuration.SecretKey) ||
                string.IsNullOrWhiteSpace(configuration.DataCenter))
                throw new InvalidOperationException(
                    $"{nameof(configuration.ApiKey)}, {nameof(configuration.SecretKey)} and {nameof(configuration.DataCenter)} must be provided");
        }
    }
}