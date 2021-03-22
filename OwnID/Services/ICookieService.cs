using System.Collections.Generic;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Flow.Contracts.Cookies;

namespace OwnID.Services
{
    public interface ICookieService
    {
        public string RecoveryCookieName { get; }

        public string EncryptionCookieName { get; }
        
        public string CredIdCookieName { get; }
        
        public CookieInfo CreateCookie(string name, string value);

        public CookieInfo DeleteCookie(string name);

        public List<CookieInfo> CreateAuthCookies(CacheItem cacheItem);

        public (CookieType? Type, string[] Values) GetVersionedCookieValues(string rawCookieValue);
    }
}