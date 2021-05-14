using System.Security.Cryptography;

namespace OwnID.Integrations.IAS
{
    public class IASConfiguration
    {
        public RSA jwtSigningCredentials { get; set;}
    }
}
