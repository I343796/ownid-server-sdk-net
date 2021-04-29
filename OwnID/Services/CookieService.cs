using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow.Contracts.Cookies;
using OwnID.Extensions;

namespace OwnID.Services
{
    public class CookieService : ICookieService
    {
        private readonly IOwnIdCoreConfiguration _configuration;
        private readonly string _domain;

        public CookieService(IOwnIdCoreConfiguration configuration)
        {
            _configuration = configuration;
            EncryptionCookieName = string.Format(CookieNameTemplates.Encryption, _configuration.CookieReference);
            RecoveryCookieName = string.Format(CookieNameTemplates.Recovery, _configuration.CookieReference);
            CredIdCookieName = string.Format(CookieNameTemplates.CredId, _configuration.CookieReference);
            _domain = _configuration.OwnIdApplicationUrl.GetWebAppBaseDomain();
        }

        public string RecoveryCookieName { get; }

        public string EncryptionCookieName { get; }

        public string CredIdCookieName { get; }

        public CookieInfo CreateCookie(string name, string value)
        {
            return new()
            {
                Name = name,
                Value = value,
                Options = GetOptions(DateTimeOffset.Now.AddDays(_configuration.CookieExpiration))
            };
        }

        public CookieInfo DeleteCookie(string name)
        {
            return new()
            {
                Name = name,
                Remove = true,
                Options = GetOptions()
            };
        }

        public List<CookieInfo> CreateAuthCookies(CacheItem cacheItem)
        {
            var result = new List<CookieInfo>();

            if (!string.IsNullOrEmpty(cacheItem.EncKey) && !string.IsNullOrEmpty(cacheItem.EncVector))
            {
                if (cacheItem.AuthCookieType != CookieType.Fido2)
                    result.Add(CreateCookie(EncryptionCookieName,
                        FormatVersionedCookieValue(cacheItem.AuthCookieType,
                            new[] {cacheItem.EncKey, cacheItem.EncVector})));
                else
                    result.Add(DeleteCookie(EncryptionCookieName));
            }


            if (!string.IsNullOrEmpty(cacheItem.RecoveryToken))
            {
                if (cacheItem.AuthCookieType != CookieType.Fido2)
                    result.Add(CreateCookie(RecoveryCookieName,
                        FormatVersionedCookieValue(CookieType.Recovery, cacheItem.RecoveryToken)));
                else
                    result.Add(DeleteCookie(RecoveryCookieName));
            }

            if (!string.IsNullOrEmpty(cacheItem.Fido2CredentialId))
                result.Add(CreateCookie(CredIdCookieName,
                    FormatVersionedCookieValue(CookieType.Fido2, cacheItem.Fido2CredentialId)));

            return result;
        }

        public (CookieType? Type, string[] Values) GetVersionedCookieValues(string rawCookieValue)
        {
            if (string.IsNullOrWhiteSpace(rawCookieValue))
                return (null, null);

            var split = rawCookieValue.Split(CookieValuesConstants.SubValueSeparator);

            if (split.Length < 3 || split[0] != CookieValuesConstants.ValueLatestVersion)
                throw new NotSupportedException($"cookie version is corrupted '{rawCookieValue}'");

            var type = split[1].ToCookieType();

            return (type, split.Skip(2).ToArray());
        }

        private CookieOptions GetOptions(DateTimeOffset? expire = null)
        {
            return new()
            {
                HttpOnly = true,
                Domain = _domain,
                Secure = !_configuration.IsDevEnvironment,
                SameSite = SameSiteMode.Lax,
                Expires = expire
            };
        }

        private string FormatVersionedCookieValue(CookieType type, string value, string version = null)
        {
            return FormatVersionedCookieValue(type, new[] {value}, version);
        }

        private string FormatVersionedCookieValue(CookieType type, string[] values, string version = null)
        {
            var starting = string.Format(CookieValuesConstants.ValueStarting,
                version ?? CookieValuesConstants.ValueLatestVersion, type.ToConstantString());

            return $"{starting}{string.Join(CookieValuesConstants.SubValueSeparator, values)}";
        }
    }
}