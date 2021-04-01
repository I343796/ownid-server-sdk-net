using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Commands;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Json;

namespace OwnID.Web.Middlewares
{
    public class AddConnectionMiddleware : BaseMiddleware
    {
        private readonly AddConnectionCommand _addConnectionCommand;

        public AddConnectionMiddleware(ILogger<AddConnectionMiddleware> logger,
            AddConnectionCommand addConnectionCommand, RequestDelegate next = null) : base(next, logger)
        {
            _addConnectionCommand = addConnectionCommand;
        }

        protected override async Task ExecuteAsync(HttpContext httpContext)
        {
            // TODO: add parsing exception handling to BaseMiddleware
            AddConnectionRequest request;
            try
            {
                request = await OwnIdSerializer.DeserializeAsync<AddConnectionRequest>(httpContext.Request.Body);
            }
            catch
            {
                BadRequest(httpContext.Response);
                return;
            }

            var result = await _addConnectionCommand.ExecuteAsync(request);
            await JsonAsync(httpContext, result, StatusCodes.Status200OK);
        }
    }
}