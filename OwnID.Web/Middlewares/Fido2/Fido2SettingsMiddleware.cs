using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Commands.Fido2;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Web.Attributes;

namespace OwnID.Web.Middlewares.Fido2
{
    [RequestDescriptor(BaseRequestFields.Context | BaseRequestFields.RequestToken)]
    public class Fido2SettingsMiddleware : BaseMiddleware
    {
        private readonly GetFido2SettingsCommand _getFido2SettingsCommand;

        public Fido2SettingsMiddleware(ILogger<Fido2SettingsMiddleware> logger,
            GetFido2SettingsCommand getFido2SettingsCommand, RequestDelegate next = null) : base(next, logger)
        {
            _getFido2SettingsCommand = getFido2SettingsCommand;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var result =
                await _getFido2SettingsCommand.ExecuteAsync(RequestIdentity.Context, RequestIdentity.RequestToken,
                    httpContext.Request.Query["l"]);
            await JsonAsync(httpContext, result, StatusCodes.Status200OK);
        }
    }
}