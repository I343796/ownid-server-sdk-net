using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Flow;
using OwnID.Flow.Interfaces;

namespace OwnID.Web.Middlewares
{
    public class StopFlowMiddleware : BaseMiddleware
    {
        private readonly IFlowRunner _flowRunner;

        public StopFlowMiddleware(ILogger<StopFlowMiddleware> logger, IFlowRunner flowRunner, RequestDelegate next = null) :
            base(next, logger)
        {
            _flowRunner = flowRunner;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var result = await _flowRunner.RunAsync(
                new TransitionInput(RequestIdentity, GetRequestCulture(httpContext), ClientDate), StepType.Stopped);
            SetCookies(httpContext.Response, result);
            await JsonAsync(httpContext, result, StatusCodes.Status200OK);
        }
    }
}