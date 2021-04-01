using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Commands.MagicLink;
using OwnID.Extensibility.Flow.Contracts.MagicLink;
using OwnID.Extensibility.Json;

namespace OwnID.Web.Middlewares.MagicLink
{
    public class ExchangeMagicLinkMiddleware : BaseMiddleware
    {
        private readonly ExchangeMagicLinkCommand _exchangeMagicLinkCommand;

        public ExchangeMagicLinkMiddleware(ILogger<ExchangeMagicLinkMiddleware> logger,
            ExchangeMagicLinkCommand exchangeMagicLinkCommand, RequestDelegate next = null) : base(next, logger)
        {
            _exchangeMagicLinkCommand = exchangeMagicLinkCommand;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var request = await OwnIdSerializer.DeserializeAsync<ExchangeMagicLinkRequest>(httpContext.Request.Body);
            await JsonAsync(httpContext, await _exchangeMagicLinkCommand.ExecuteAsync(request),
                StatusCodes.Status200OK);
        }
    }
}