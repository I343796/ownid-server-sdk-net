using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using OwnID.Configuration.Profile;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Configuration.Profile;
using OwnID.Extensions;

namespace OwnID.Configuration
{
    /// <summary>
    ///     Core configuration that required for basic functioning
    /// </summary>
    /// <remarks>
    ///     Implements <see cref="IDisposable" />
    /// </remarks>
    /// <inheritdoc cref="IOwnIdCoreConfiguration" />
    public class OwnIdCoreConfiguration : IOwnIdCoreConfiguration, IDisposable
    {
        private string _did;
        private string _cookieReference;

        /// <summary>
        ///     Creates instance with default parameters
        /// </summary>
        /// <remarks>
        ///     Calls <see cref="OwnIdCoreConfiguration(System.Type)" /> with <see cref="DefaultProfileModel" />
        /// </remarks>
        public OwnIdCoreConfiguration() : this(typeof(DefaultProfileModel))
        {
        }

        /// <summary>
        ///     Creates instance with provided Profile Type
        /// </summary>
        /// <param name="profileType">Value for <see cref="ProfileConfiguration" /> instantiation</param>
        public OwnIdCoreConfiguration(Type profileType)
        {
            OwnIdApplicationUrl = new Uri(Constants.OwinIdApplicationAddress);
            ProfileConfiguration = new ProfileConfiguration(profileType);
        }

        // public string RegisterInstructions { get; set; }
        //
        // public string LoginInstructions { get; set; }

        /// <summary>
        ///     Disposes <see cref="JwtSignCredentials" />
        /// </summary>
        public void Dispose()
        {
            JwtSignCredentials?.Dispose();
        }

        public Uri OwnIdApplicationUrl { get; set; }

        public Uri CallbackUrl { get; set; }

        public RSA JwtSignCredentials { get; set; }

        public IProfileConfiguration ProfileConfiguration { get; set; }

        public string DID
        {
            get => _did;
            set
            {
                _did = value;
                _cookieReference = _did.SanitizeCookieName();
            }
        }

        public string Name { get; set; }

        public string Icon { get; set; }

        public string Description { get; set; }

        public int PollingInterval { get; set; }

        public bool IsDevEnvironment { get; set; }

        public uint CacheExpirationTimeout { get; set; }

        public uint MaximumNumberOfConnectedDevices { get; set; }

        public uint JwtExpirationTimeout { get; set; }

        public bool OverwriteFields { get; set; }

        public string CookieReference => _cookieReference;
        
        public string TopDomain { get; set; }

        public bool TFAEnabled { get; set; } = true;

        public Fido2FallbackBehavior Fido2FallbackBehavior { get; set; } = Fido2FallbackBehavior.Passcode;

        public IFido2Configuration Fido2 { get; set; } = new Fido2Configuration();

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        ///     Sets <see cref="ProfileConfiguration" /> based on <typeparamref name="TProfile" />
        /// </summary>
        /// <typeparam name="TProfile">User Profile mode type</typeparam>
        public void SetProfileModel<TProfile>() where TProfile : class
        {
            ProfileConfiguration = new ProfileConfiguration(typeof(TProfile));
        }
    }
}