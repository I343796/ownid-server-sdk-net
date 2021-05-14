using Microsoft.AspNetCore.Http;
using OwnID.Extensibility.Json;
using OwnID.Integrations.IAS.Certs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace OwnID.Integrations.IAS
{
    public class CertDiscovery
    {
        RequestDelegate _next;
        private readonly IASConfiguration _configuration;
        public CertDiscovery(RequestDelegate next, IASConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            RsaSecurityKey jwtSignCredentials = new RsaSecurityKey(_configuration.jwtSigningCredentials);
            JsonWebKey parsedJwK = JsonWebKeyConverter.ConvertFromRSASecurityKey(jwtSignCredentials);
            
            JwkWrapper key = new JwkWrapper(parsedJwK);
            JwkWrapper[] keys = new JwkWrapper[] { key };
            var result = new Dictionary<string, object>() 
            {
                {"keys", keys}
            };


            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(OwnIdSerializer.Serialize(result));

        }
    }
}
