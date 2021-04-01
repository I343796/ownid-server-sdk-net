using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Commands.MagicLink;

namespace OwnID.Web.Middlewares.MagicLink
{
    public class SendMagicLinkMiddleware : BaseMiddleware
    {
        private readonly SendMagicLinkCommand _sendMagicLinkCommand;

        public SendMagicLinkMiddleware(ILogger<SendMagicLinkMiddleware> logger,
            SendMagicLinkCommand sendMagicLinkCommand, RequestDelegate next = null) : base(next, logger)
        {
            _sendMagicLinkCommand = sendMagicLinkCommand;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var email = httpContext.Request.Query["email"];

            await JsonAsync(httpContext, await _sendMagicLinkCommand.ExecuteAsync(email), StatusCodes.Status200OK);
        }
    }
}