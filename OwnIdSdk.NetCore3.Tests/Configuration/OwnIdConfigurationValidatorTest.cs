using System;
using System.Security.Cryptography;
using OwnIdSdk.NetCore3.Configuration;
using Xunit;

namespace OwnIdSdk.NetCore3.Tests.Configuration
{
    public class OwnIdConfigurationValidatorTest : IDisposable
    {
        private readonly RSA _sign;
        private readonly OwnIdConfigurationValidator _validator;
        
        public OwnIdConfigurationValidatorTest()
        {
            _sign = RSA.Create();
            _validator = new OwnIdConfigurationValidator();
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
            Assert.True(_validator.Validate(string.Empty, config).Failed);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("http://test.com/")]
        [InlineData("https://www.test.com/search/Le+Venezuela+b%C3%A9n%C3%A9ficie+d%27importantes+ressources+naturelles+%3A+p%C3%A9trole%2C+gaz%2C+mines")]
        [InlineData("https://test.com/?a=1")]
        public void Validate_Invalid_OwnIdApplicationUrl(string value)
        {
            var config = GetValidConfiguration();
            config.OwnIdApplicationUrl = value != null ? new Uri(value) : null;
            Assert.True(_validator.Validate(string.Empty, config).Failed);
        }

        [Fact]
        public void Validate_Invalid_JwtSignCredentials()
        {
            var config = GetValidConfiguration();
            config.JwtSignCredentials = null;
            Assert.True(_validator.Validate(string.Empty, config).Failed);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Validate_Invalid_Requester_DID(string value)
        {
            var config = GetValidConfiguration();
            config.Requester.DID = value;
            Assert.True(_validator.Validate(string.Empty, config).Failed);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Validate_Invalid_Requester_Name(string value)
        {
            var config = GetValidConfiguration();
            config.Requester.Name = value;
            Assert.True(_validator.Validate(string.Empty, config).Failed);
        }
        
        private OwnIdConfiguration GetValidConfiguration()
        {
            return new OwnIdConfiguration
            {
                OwnIdApplicationUrl = new Uri("https://ownid.com:12/sign"),
                CallbackUrl = new Uri("https://localhost:12"),
                Requester = { DID = "did", Name = "name"},
                JwtSignCredentials = _sign
            };
        }

        public void Dispose()
        {
            _sign?.Dispose();
        }
    }
}