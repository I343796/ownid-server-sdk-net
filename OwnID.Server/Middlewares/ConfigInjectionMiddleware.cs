using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Json;

namespace OwnID.Server.Middlewares
{
    public class ConfigInjectionMiddleware
    {
        private readonly IOwnIdCoreConfiguration _configuration;

        public ConfigInjectionMiddleware(RequestDelegate next, IOwnIdCoreConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var config = await OwnIdSerializer.DeserializeAsync<ConfigToInject>(context.Request.Body);

            _configuration.TFAEnabled = config.TFAEnabled;
            _configuration.Fido2FallbackBehavior = config.Fido2FallbackBehavior;

            context.Response.StatusCode = StatusCodes.Status204NoContent;
        }

        private class ConfigToInject
        {
            public bool TFAEnabled { get; set; }
            public Fido2FallbackBehavior Fido2FallbackBehavior { get; set; }
        }
    }
}