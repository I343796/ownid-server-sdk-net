using System;
using System.Security.Cryptography;
using FluentAssertions;
using OwnID.Configuration;
using OwnID.Configuration.Validators;
using Xunit;

namespace OwnID.Tests.Configuration
{
    public class OwnIdConfigurationValidatorTest : IDisposable
    {
        private readonly RSA _sign;
        private readonly OwnIDCoreConfigurationValidator _validator;

        public OwnIdConfigurationValidatorTest()
        {
            _sign = RSA.Create();
            _validator = new OwnIDCoreConfigurationValidator();
        }

        public void Dispose()
        {
            _sign?.Dispose();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("http://test.com/")]
        [InlineData("https://www.test.com/search/Le+Venezuela+b%C3%A9n%C3%A9ficie+d%27importantes+ressources+naturelles+%3A+p%C3%A9trole%2C+gaz%2C+mines")]
        [InlineData("https://test.com/?a=1")]
        public void Validate_Invalid_CallbackUrl(string value)
        {
            var config = GetValidConfiguration();
            config.OwnIdApplicationUrl = value != null ? new Uri(value) : null;
            _validator.ValidateInternal(config).Failed.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("http://test.com/")]
        [InlineData(
            "https://www.test.com/search/Le+Venezuela+b%C3%A9n%C3%A9ficie+d%27importantes+ressources+naturelles+%3A+p%C3%A9trole%2C+gaz%2C+mines")]
        [InlineData("https://test.com/?a=1")]
        public void Validate_Invalid_OwnIdApplicationUrl(string value)
        {
            var config = GetValidConfiguration();
            config.OwnIdApplicationUrl = value != null ? new Uri(value) : null;
            _validator.ValidateInternal(config).Failed.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Validate_Invalid_Requester_DID(string value)
        {
            var config = GetValidConfiguration();
            config.DID = value;
            _validator.ValidateInternal(config).Failed.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Validate_Invalid_Requester_Name(string value)
        {
            var config = GetValidConfiguration();
            config.Name = value;
            _validator.ValidateInternal(config).Failed.Should().BeTrue();
        }

        private OwnIdCoreConfiguration GetValidConfiguration()
        {
            var config = new OwnIdCoreConfiguration
            {
                OwnIdApplicationUrl = new Uri("https://ownid.com:12/sign"),
                CallbackUrl = new Uri("https://localhost:12"),
                DID = "did",
                Name = "name",
                JwtSignCredentials = _sign,
                CacheExpirationTimeout = 600000,
                JwtExpirationTimeout = 60000,
                TopDomain = "ownid.com"
            };

            config.Fido2.Origin = new Uri("https://test.com");
            config.Fido2.PasswordlessPageUrl = new Uri("https://test.com");

            return config;
        }

        [Fact]
        public void Validate_Invalid_JwtSignCredentials()
        {
            var config = GetValidConfiguration();
            config.JwtSignCredentials = null;
            _validator.ValidateInternal(config).Failed.Should().BeTrue();
        }
    }
}