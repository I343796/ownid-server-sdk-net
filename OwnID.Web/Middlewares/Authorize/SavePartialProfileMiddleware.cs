using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Flow;
using OwnID.Flow.Interfaces;
using OwnID.Web.Attributes;

namespace OwnID.Web.Middlewares.Authorize
{
    [RequestDescriptor(BaseRequestFields.Context | BaseRequestFields.RequestToken |
                       BaseRequestFields.ResponseToken)]
    public class SavePartialProfileMiddleware : BaseMiddleware
    {
        private readonly IFlowRunner _flowRunner;

        public SavePartialProfileMiddleware(IFlowRunner flowRunner,
            ILogger<SavePartialProfileMiddleware> logger, RequestDelegate next = null) : base(next, logger)
        {
            _flowRunner = flowRunner;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var jwtContainer = await GetRequestJwtContainerAsync(httpContext);
            var result = await _flowRunner.RunAsync(
                new TransitionInput<JwtContainer>(RequestIdentity, GetRequestCulture(httpContext), jwtContainer,
                    ClientDate),
                StepType.InstantAuthorize);
            
            SetCookies(httpContext.Response, result);
            await JsonAsync(httpContext, result, StatusCodes.Status200OK);
        }
    }
}