using System;
using Microsoft.Extensions.DependencyInjection;
using OwnID.Configuration;
using OwnID.Configuration.Validators;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Configuration.Validators;
using OwnID.Extensibility.Services;
using OwnID.Services;
using OwnID.Web.Extensibility;

namespace OwnID.Web.Features
{
    public class EmailFeature : IFeature
    {
        private readonly ISmtpConfiguration _configuration = new SmtpConfiguration();
        private readonly IConfigurationValidator<ISmtpConfiguration> _validator = new SmtpConfigurationValidator();

        public EmailFeature WithConfiguration(Action<ISmtpConfiguration> setupAction)
        {
            setupAction(_configuration);
            return this;
        }

        public void ApplyServices(IServiceCollection services)
        {
            services.AddSingleton(_configuration);
            services.AddSingleton<IEmailService, EmailService>();
        }

        public IFeature FillEmptyWithOptional()
        {
           _validator.FillEmptyWithOptional(_configuration);

            return this;
        }

        public void Validate()
        {
           _validator.Validate(_configuration);
        }
    }
}