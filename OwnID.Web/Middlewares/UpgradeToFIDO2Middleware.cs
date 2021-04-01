using System.IO;
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
    public class UpgradeToFIDO2Middleware : BaseMiddleware
    {
        public const string NewAuthTypeRouteName = "authType";
        private readonly IFlowRunner _flowRunner;

        public UpgradeToFIDO2Middleware(ILogger<UpgradeToFIDO2Middleware> logger,
            IFlowRunner flowRunner, RequestDelegate next = null) : base(next, logger)
        {
            _flowRunner = flowRunner;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            using var bodyReader = new StreamReader(httpContext.Request.Body);
            var bodyStr = await bodyReader.ReadToEndAsync();

            var fidoResult = await _flowRunner.RunAsync(
                new TransitionInput<string>(RequestIdentity, GetRequestCulture(httpContext), bodyStr, ClientDate),
                StepType.UpgradeToFido2);

            SetCookies(httpContext.Response, fidoResult);
            await JsonAsync(httpContext, fidoResult, StatusCodes.Status200OK);
        }
    }
}