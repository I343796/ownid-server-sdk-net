using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Flow;
using OwnID.Flow.Interfaces;
using OwnID.Web.Attributes;

namespace OwnID.Web.Middlewares
{
    [RequestDescriptor(BaseRequestFields.Context | BaseRequestFields.RequestToken | BaseRequestFields.ResponseToken)]
    public class InternalConnectionRecoveryMiddleware : BaseMiddleware
    {
        private readonly IFlowRunner _flowRunner;

        public InternalConnectionRecoveryMiddleware(ILogger<InternalConnectionRecoveryMiddleware> logger, IFlowRunner flowRunner, RequestDelegate next = null) : base(next, logger)
        {
            _flowRunner = flowRunner;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var input = new TransitionInput(RequestIdentity, GetRequestCulture(httpContext), ClientDate);

            var result = await _flowRunner.RunAsync(input, StepType.InternalConnectionRecovery);

            await JsonAsync(httpContext, result, StatusCodes.Status200OK);
        }
    }
}