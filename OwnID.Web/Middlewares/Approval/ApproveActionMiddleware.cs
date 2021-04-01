using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Commands.Pin;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Json;
using OwnID.Web.Attributes;

namespace OwnID.Web.Middlewares.Approval
{
    [RequestDescriptor(BaseRequestFields.Context)]
    public class ApproveActionMiddleware : BaseMiddleware
    {
        private readonly ApproveActionCommand _approveActionCommand;

        public ApproveActionMiddleware(ApproveActionCommand approveActionCommand,
            ILogger<ApproveActionMiddleware> logger, RequestDelegate next = null) : base(next, logger)
        {
            _approveActionCommand = approveActionCommand;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            var request = await OwnIdSerializer.DeserializeAsync<ApproveActionRequest>(httpContext.Request.Body);

            await _approveActionCommand.ExecuteAsync(request);
            OkNoContent(httpContext.Response);
        }
    }
}