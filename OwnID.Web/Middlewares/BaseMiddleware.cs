using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Extensibility.Json;
using OwnID.Extensions;
using OwnID.Web.Attributes;

namespace OwnID.Web.Middlewares
{
    public abstract class BaseMiddleware : IOwnIDMiddleware
    {
        private const string ClientDateParameterName = "t";
        private const string ContextRouteName = "context";

        private readonly BaseRequestFields _fields;

        protected readonly ILogger Logger;
        protected readonly RequestDelegate Next;
        protected DateTime ClientDate { get; private set; }


        protected BaseMiddleware(RequestDelegate next, ILogger logger)
        {
            Next = next;
            Logger = logger;

            var attrs = GetType().GetCustomAttribute(typeof(RequestDescriptorAttribute));

            if (attrs != null && attrs is RequestDescriptorAttribute descriptor)
                _fields = descriptor.Fields;
            else
                _fields = BaseRequestFields.None;
        }

        protected RequestIdentity RequestIdentity { get; private set; }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            RequestIdentity = new RequestIdentity
            {
                Context = httpContext.GetRouteData().Values[ContextRouteName]?.ToString(),
            };

            using var scope = Logger.BeginScope("context: {context}", RequestIdentity.Context);

            ClientDate = httpContext.Request.Query[ClientDateParameterName].Count > 0
                         && DateTime.TryParse(httpContext.Request.Query[ClientDateParameterName][0],
                             out var clientDate)
                ? clientDate.ToUniversalTime()
                : DateTime.UtcNow;

            await InterceptErrors(InternalExecuteAsync, httpContext);
        }

        protected abstract Task ExecuteAsync(HttpContext httpContext);

        protected async Task<JwtContainer> GetRequestJwtContainerAsync(HttpContext httpContext)
        {
            var jwtContainer = await OwnIdSerializer.DeserializeAsync<JwtContainer>(httpContext.Request.Body);

            if (string.IsNullOrEmpty(jwtContainer?.Jwt))
                throw new CommandValidationException("No JWT was found in request");

            return jwtContainer;
        }

        protected CultureInfo GetRequestCulture(HttpContext context)
        {
            var rqf = context.Features.Get<IRequestCultureFeature>();
            return rqf.RequestCulture.Culture;
        }

        private async Task InternalExecuteAsync(HttpContext httpContext)
        {
            if (_fields != BaseRequestFields.None)
            {
                if (_fields.HasFlag(BaseRequestFields.Context) &&
                    string.IsNullOrEmpty(RequestIdentity.Context))
                    throw new CommandValidationException(
                        $"{nameof(RequestIdentity.Context)} is required and should have correct format. Path={httpContext.Request.Path.ToString()}");

                if (_fields.HasFlag(BaseRequestFields.RequestToken))
                {
                    if (httpContext.Request.Query.TryGetValue("rt", out var requestToken))
                        RequestIdentity.RequestToken = requestToken;
                    else
                        throw new CommandValidationException(
                            $"{nameof(RequestIdentity.RequestToken)} is required. Path={httpContext.Request.Path.ToString()} Query={httpContext.Request.QueryString.ToString()}");
                }

                if (_fields.HasFlag(BaseRequestFields.ResponseToken))
                {
                    if (httpContext.Request.Query.TryGetValue("rst", out var responseToken))
                        RequestIdentity.ResponseToken = responseToken.ToString().EncodeBase64String();
                    else
                        throw new CommandValidationException(
                            $"{nameof(RequestIdentity.ResponseToken)} is required. Path={httpContext.Request.Path.ToString()} Query={httpContext.Request.QueryString.ToString()}");
                }
            }

            await ExecuteAsync(httpContext);
        }

        private async Task InterceptErrors(Func<HttpContext, Task> functionToInvoke, HttpContext httpContext)
        {
            try
            {
                await functionToInvoke(httpContext);
            }
            catch (InternalLogicException e)
            {
                Logger.LogError(e, e.Message);
                httpContext.Response.Clear();
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
            catch (CommandValidationException e)
            {
                Logger.LogError(e, e.Message);
                httpContext.Response.Clear();
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            }
            catch (BusinessValidationException e)
            {
                var response = new BadRequestResponse
                {
                    FieldErrors = e.FormContext.FieldErrors as IDictionary<string, IList<string>>,
                    GeneralErrors = e.FormContext.GeneralErrors
                };
                httpContext.Response.Clear();
                await JsonAsync(httpContext, response, StatusCodes.Status400BadRequest);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Middleware error interceptor");
                httpContext.Response.Clear();
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                httpContext.Response.ContentType = "text/html";
                await httpContext.Response.WriteAsync("Operation failed. Please try later");
            }
        }

        protected void SetCookies(HttpResponse httpResponse, ITransitionResult result)
        {
            if(!(result is StateResult))
                return;

            var cookies = ((StateResult) result).Cookies;
            
            foreach (var cookie in cookies)
            {
                if(!cookie.Remove)
                    httpResponse.Cookies.Append(cookie.Name, cookie.Value, cookie.Options);
                else
                    httpResponse.Cookies.Delete(cookie.Name, cookie.Options);
            }
        }

        #region Response Shortcuts

        protected void OkNoContent(HttpResponse response)
        {
            response.StatusCode = (int) HttpStatusCode.NoContent;
        }

        protected async Task JsonAsync<T>(HttpContext context, T responseBody, int statusCode, bool addLocaleHeader = true)
            where T : class
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            if (addLocaleHeader)
            {
                var culture = GetRequestCulture(context);
                context.Response.Headers.Add("Content-Language", culture.ToString());
            }

            await context.Response.WriteAsync(OwnIdSerializer.Serialize(responseBody));
        }

        protected void BadRequest(HttpResponse response)
        {
            response.StatusCode = (int) HttpStatusCode.BadRequest;
        }

        #endregion
    }
}