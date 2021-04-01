using System;
using FluentAssertions;
using OwnID.Configuration;
using OwnID.Configuration.Validators;
using Xunit;

namespace OwnID.Tests.Configuration
{
    public class Fido2ConfigurationValidatorTest
    {
        private readonly Fido2ConfigurationValidator _validator;
        
        public Fido2ConfigurationValidatorTest()
        {
            _validator = new Fido2ConfigurationValidator();
        }


        [Fact]
        public void Validate_Success_Empty()
        {
            var config = new Fido2Configuration();
            var result = _validator.Validate(config, false);
            result.Failed.Should().BeFalse();
        }
        
        [Theory]
        [InlineData(true, @"c:\dir\file", "{0} is not valid url")]
        [InlineData(false, "http://ownid.com", "{0}: https is required for production use")]
        [InlineData(true, "ftp://ownid.com", "{0}: https or http are supported only")]
        [InlineData(true, "http://ownid.com?param=val", "{0} should not contain query params")]
        public void Validate_Invalid_Fido2Url(bool isDev, string fido2Url, string errorMessage)
        {
            var config = GetValidConfiguration();
            config.PasswordlessPageUrl = !string.IsNullOrEmpty(fido2Url) ? new Uri(fido2Url) : null;

            var result = _validator.Validate(config, isDev);
            var expectedError = string.Format(errorMessage, nameof(config.PasswordlessPageUrl));

            result.Failed.Should().BeTrue();
            result.FailureMessage.Should().BeEquivalentTo(expectedError);
        }

        [Theory]
        [InlineData(true, @"http://ownid.com")]
        [InlineData(false, "https://ownid.com")]
        public void Validate_Valid_Fido2Url(bool isDev, string fido2Url)
        {
            var config = GetValidConfiguration();
            config.PasswordlessPageUrl = new Uri(fido2Url);
            
            var result = _validator.Validate(config, isDev);

            result.Succeeded.Should().BeTrue();
        }

        [Theory]
        [InlineData(true, null, "{0} is required")]
        [InlineData(true, @"c:\dir\file", "{0} is not valid url")]
        [InlineData(false, "http://ownid.com", "{0}: https is required for production use")]
        [InlineData(true, "ftp://ownid.com", "{0}: https or http are supported only")]
        [InlineData(true, "http://ownid.com?param=val", "{0} should not contain query params")]
        public void Validate_Invalid_Origin(bool isDev, string originUrl, string errorMessage)
        {
            var config = GetValidConfiguration();

            config.Origin = !string.IsNullOrEmpty(originUrl) ? new Uri(originUrl) : null;

            var result = _validator.Validate(config, isDev);
            var expectedError = string.Format(errorMessage, nameof(config.Origin));

            result.Failed.Should().BeTrue();
            result.FailureMessage.Should().BeEquivalentTo(expectedError);
        }

        [Theory]
        [InlineData(true, @"http://ownid.com")]
        [InlineData(false, "https://ownid.com")]
        public void Validate_Valid_Origin(bool isDev, string fido2Url)
        {
            var config = GetValidConfiguration();
            config.Origin = new Uri(fido2Url);
            
            var result = _validator.Validate(config, isDev);

            result.Succeeded.Should().BeTrue();
        }
        
        private Fido2Configuration GetValidConfiguration()
        {
            return new()
            {
                PasswordlessPageUrl = new Uri("https://localhost:5002/ownid"),
                Origin = new Uri("https://localhost:5002"),
                UserName = "testUserName",
                RelyingPartyId = "testRelyingPartyId",
                RelyingPartyName = "testRelyingPartyName",
                UserDisplayName = "testUserDisplayName"
            };
        }
    }
}