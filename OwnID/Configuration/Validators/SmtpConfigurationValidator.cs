using System;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Configuration.Validators;

namespace OwnID.Configuration.Validators
{
    public class SmtpConfigurationValidator : IConfigurationValidator<ISmtpConfiguration>
    {
        public void FillEmptyWithOptional(ISmtpConfiguration configuration)
        {
            if (configuration.Port == 0)
                configuration.Port = 465;

            configuration.FromName ??= string.Empty;
        }

        public void Validate(ISmtpConfiguration configuration)
        {
            if(string.IsNullOrWhiteSpace(configuration.FromAddress))
                throw new InvalidOperationException($"Smpt.{nameof(configuration.FromAddress)} is required");
            
            if(string.IsNullOrWhiteSpace(configuration.Host))
                throw new InvalidOperationException($"Smpt.{nameof(configuration.Host)} is required");
        }
    }
}