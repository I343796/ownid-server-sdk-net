using Microsoft.IdentityModel.Tokens;

namespace OwnID.Integrations.IAS.Certs
{
    public class JwkWrapper
    { 
        public JwkWrapper(JsonWebKey jwk)
        {
            this.kid = Base64UrlEncoder.Encode(jwk.ComputeJwkThumbprint());
            this.n = jwk.N;
            this.e = jwk.E;
            this.d = jwk.D;
            this.kty = jwk.Kty;
            this.use = jwk.Use;
        }
        public string use { get; set; }
        public string alg { get; set; }
        public string e { get; set; }
        public string d { get; set; }
        public string n { get; set; }
        public string kid { get; set; }
        public string kty { get; set; }
        
    }
}
