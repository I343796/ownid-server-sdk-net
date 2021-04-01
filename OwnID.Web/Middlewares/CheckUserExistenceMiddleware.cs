using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Json;
using OwnID.Flow;
using OwnID.Flow.Interfaces;
using OwnID.Web.Attributes;

namespace OwnID.Web.Middlewares
{
    [RequestDescriptor(BaseRequestFields.Context | BaseRequestFields.RequestToken | BaseRequestFields.ResponseToken)]
    public class CheckUserExistenceMiddleware : BaseMiddleware
    {
        private readonly IFlowRunner _flowRunner;

        public CheckUserExistenceMiddleware(ILogger<CheckUserExistenceMiddleware> logger,
            IFlowRunner flowRunner, RequestDelegate next = null) : base(next, logger)
        {
            _flowRunner = flowRunner;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var request = await OwnIdSerializer.DeserializeAsync<UserIdentification>(httpContext.Request.Body);

            var commandInput = new TransitionInput<UserIdentification>(RequestIdentity, GetRequestCulture(httpContext),
                request, ClientDate);

            var result = await _flowRunner.RunAsync(commandInput, StepType.CheckUserExistence);
            await JsonAsync(httpContext, result, StatusCodes.Status200OK);
        }
    }
}