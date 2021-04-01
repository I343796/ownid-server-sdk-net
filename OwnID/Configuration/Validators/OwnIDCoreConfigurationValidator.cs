using System;
using Microsoft.Extensions.Options;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Configuration.Validators;

namespace OwnID.Configuration.Validators
{
    public class OwnIDCoreConfigurationValidator : IConfigurationValidator<IOwnIdCoreConfiguration>
    {
        public void FillEmptyWithOptional(IOwnIdCoreConfiguration configuration)
        {
            configuration.OwnIdApplicationUrl ??= new Uri(Constants.OwinIdApplicationAddress);

            if (configuration.CacheExpirationTimeout == default)
                configuration.CacheExpirationTimeout = (uint) TimeSpan.FromMinutes(10).TotalMilliseconds;

            if (configuration.JwtExpirationTimeout == default)
                configuration.JwtExpirationTimeout = (uint) TimeSpan.FromMinutes(60).TotalMilliseconds;

            if (configuration.PollingInterval == default)
                configuration.PollingInterval = 2000;

            if (configuration.MaximumNumberOfConnectedDevices == default)
                configuration.MaximumNumberOfConnectedDevices = 1;

            if (string.IsNullOrWhiteSpace(configuration.Fido2.RelyingPartyId))
                configuration.Fido2.RelyingPartyId = configuration.Fido2.PasswordlessPageUrl?.Host;

            if (string.IsNullOrWhiteSpace(configuration.Fido2.RelyingPartyName))
                configuration.Fido2.RelyingPartyName = configuration.Name;

            if (string.IsNullOrWhiteSpace(configuration.Fido2.UserName))
                configuration.Fido2.UserName = "Skip the password";

            if (string.IsNullOrWhiteSpace(configuration.Fido2.UserDisplayName))
                configuration.Fido2.UserDisplayName = configuration.Fido2.UserName;

            if (configuration.Fido2.Origin == null)
                configuration.Fido2.Origin = configuration.Fido2.PasswordlessPageUrl;
        }

        public void Validate(IOwnIdCoreConfiguration configuration)
        {
            // TODO refactor
            var result = ValidateInternal(configuration);

            if (result.Failed)
                throw new InvalidOperationException(result.FailureMessage);
        }

        public ValidateOptionsResult ValidateInternal(IOwnIdCoreConfiguration options)
        {
            if (!UriValidationHelper.IsValid(nameof(options.CallbackUrl), options.CallbackUrl, options.IsDevEnvironment,
                out var callBackUrlValidationError))
                return ValidateOptionsResult.Fail(callBackUrlValidationError);

            if (!UriValidationHelper.IsValid(nameof(options.OwnIdApplicationUrl), options.OwnIdApplicationUrl, options.IsDevEnvironment,
                out var ownIdAppUrlValidationError))
                return ValidateOptionsResult.Fail(ownIdAppUrlValidationError);

            if (options.JwtSignCredentials == default)
                return ValidateOptionsResult.Fail($"{nameof(options.JwtSignCredentials)} are required");

            if (string.IsNullOrWhiteSpace(options.DID) || string.IsNullOrWhiteSpace(options.Name))
                return ValidateOptionsResult.Fail(
                    $"{nameof(options.DID)} and {nameof(options.Name)} are required");

            if (options.CacheExpirationTimeout == 0)
                return ValidateOptionsResult.Fail(
                    $"{nameof(options.CacheExpirationTimeout)} can not be equal to 0");

            if (options.JwtExpirationTimeout == 0)
                return ValidateOptionsResult.Fail(
                    $"{nameof(options.JwtExpirationTimeout)} can not be equal to 0");

            if (string.IsNullOrWhiteSpace(options.TopDomain))
                return ValidateOptionsResult.Fail($"{nameof(options.TopDomain)} is required");

            // Validate Fido2 configuration
            var fido2Validator = new Fido2ConfigurationValidator();
            var fido2ValidationResult = fido2Validator.Validate(options.Fido2, options.IsDevEnvironment);
            if (fido2ValidationResult.Failed)
                return fido2ValidationResult;

            if (!options.Fido2.IsEnabled && options.Fido2FallbackBehavior == Fido2FallbackBehavior.Block)
            {
                return ValidateOptionsResult.Fail(
                    $"FIDO2 is disabled, but '{nameof(options.Fido2FallbackBehavior)}' is set to '{nameof(Fido2FallbackBehavior.Block)}'");
            }

            
            if (options.ProfileConfiguration != null)
                return options.ProfileConfiguration.Validate();

            return ValidateOptionsResult.Success;
        }
    }
}