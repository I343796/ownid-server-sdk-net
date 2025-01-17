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
    public class SaveProfileMiddleware : BaseMiddleware
    {
        private readonly IFlowRunner _flowRunner;

        public SaveProfileMiddleware(IFlowRunner flowRunner,
            ILogger<SaveProfileMiddleware> logger, RequestDelegate next = null) : base(next, logger)
        {
            _flowRunner = flowRunner;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var jwtContainer = await GetRequestJwtContainerAsync(httpContext);
            var result = await _flowRunner.RunAsync(
                new TransitionInput<JwtContainer>(RequestIdentity, GetRequestCulture(httpContext), jwtContainer,
                    ClientDate),
                StepType.Authorize);
            
            SetCookies(httpContext.Response, result);
            await JsonAsync(httpContext, result, StatusCodes.Status200OK);
        }
    }
}