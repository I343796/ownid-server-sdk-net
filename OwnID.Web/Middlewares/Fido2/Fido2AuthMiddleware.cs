using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Flow;
using OwnID.Flow.Interfaces;
using OwnID.Web.Attributes;

namespace OwnID.Web.Middlewares.Fido2
{
    [RequestDescriptor(BaseRequestFields.Context | BaseRequestFields.RequestToken | BaseRequestFields.ResponseToken)]
    public class Fido2AuthMiddleware : BaseMiddleware
    {
        private readonly IFlowRunner _flowRunner;

        public Fido2AuthMiddleware(IFlowRunner flowRunner, ILogger<Fido2AuthMiddleware> logger, RequestDelegate next = null) :
            base(next, logger)
        {
            _flowRunner = flowRunner;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            using var bodyReader = new StreamReader(httpContext.Request.Body);
            var bodyStr = await bodyReader.ReadToEndAsync();

            var result = await _flowRunner.RunAsync(
                new TransitionInput<string>(RequestIdentity, GetRequestCulture(httpContext), bodyStr, ClientDate),
                StepType.Fido2Authorize);

            SetCookies(httpContext.Response, result);
            await JsonAsync(httpContext, result, StatusCodes.Status200OK);
        }
    }
}