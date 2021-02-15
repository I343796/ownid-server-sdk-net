using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Flow;
using OwnID.Flow.Interfaces;
using OwnID.Services;
using OwnID.Web.Attributes;

namespace OwnID.Web.Middlewares
{
    [RequestDescriptor(BaseRequestFields.Context | BaseRequestFields.RequestToken)]
    public class StartFlowMiddleware : BaseMiddleware
    {
        private readonly ICookieService _cookieService;
        private readonly IFlowRunner _flowRunner;

        public StartFlowMiddleware(RequestDelegate next, IFlowRunner flowRunner, ILogger<StartFlowMiddleware> logger,
            ICookieService cookieService) : base(next, logger)
        {
            _flowRunner = flowRunner;
            _cookieService = cookieService;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var request = new StartRequest
            {
                EncryptionToken = httpContext.Request.Cookies[_cookieService.EncryptionCookieName],
                RecoveryToken = httpContext.Request.Cookies[_cookieService.RecoveryCookieName],
                CredId = httpContext.Request.Cookies[_cookieService.CredIdCookieName]
            };
            
            Logger.Log(LogLevel.Debug, $"StartFlowMiddleware -> CredId -> {request.CredId}");

            if (httpContext.Request.Query.TryGetValue("rst", out var responseToken))
                RequestIdentity.ResponseToken = responseToken;

            var commandInput = new TransitionInput<StartRequest>(RequestIdentity, GetRequestCulture(httpContext),
                request, ClientDate);

            var commandResult = await _flowRunner.RunAsync(commandInput, StepType.Starting);
            await JsonAsync(httpContext, commandResult, StatusCodes.Status200OK);
        }
    }
}