using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Extensibility.Json;
using OwnID.Flow;
using OwnID.Flow.Interfaces;
using OwnID.Web.Attributes;

namespace OwnID.Web.Middlewares
{
    [RequestDescriptor(BaseRequestFields.Context | BaseRequestFields.RequestToken | BaseRequestFields.ResponseToken)]
    public class AcceptStartFlowMiddleware : BaseMiddleware
    {
        private readonly IFlowRunner _flowRunner;

        public AcceptStartFlowMiddleware(ILogger<AcceptStartFlowMiddleware> logger,
            IFlowRunner flowRunner, RequestDelegate next = null) : base(next, logger)
        {
            _flowRunner = flowRunner;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var request = await OwnIdSerializer.DeserializeAsync<AcceptStartRequest>(httpContext.Request.Body);
            var input = new TransitionInput<AcceptStartRequest>(RequestIdentity, GetRequestCulture(httpContext),
                request, ClientDate);

            var result = await _flowRunner.RunAsync(input, StepType.AcceptStart);
            await JsonAsync(httpContext, result, StatusCodes.Status200OK);
        }
    }
}