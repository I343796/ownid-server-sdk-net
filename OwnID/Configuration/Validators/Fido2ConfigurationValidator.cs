using Microsoft.Extensions.Options;
using OwnID.Extensibility.Configuration;

namespace OwnID.Configuration.Validators
{
    public class Fido2ConfigurationValidator
    {
        public ValidateOptionsResult Validate(IFido2Configuration configuration, bool isDevEnvironment)
        {
            // Validate Fido2Url
            if (configuration.IsEnabled && !UriValidationHelper.IsValid(
                nameof(configuration.PasswordlessPageUrl),
                configuration.PasswordlessPageUrl, isDevEnvironment, out var error))
                return ValidateOptionsResult.Fail(error);
            
            // Validate Origin
            if (configuration.IsEnabled && !UriValidationHelper.IsValid(
                nameof(configuration.Origin),
                configuration.Origin, isDevEnvironment, out var errorOrigin))
                return ValidateOptionsResult.Fail(errorOrigin);
            
            // Validate RelyingPartyId
            if(configuration.IsEnabled && string.IsNullOrEmpty(configuration.RelyingPartyId))
                return ValidateOptionsResult.Fail($"{nameof(configuration.RelyingPartyId)} is required");
            
            // Validate UserName
            if(configuration.IsEnabled && string.IsNullOrEmpty(configuration.UserName))
                return ValidateOptionsResult.Fail($"{nameof(configuration.UserName)} is required");

            return ValidateOptionsResult.Success;
        }
    }
}