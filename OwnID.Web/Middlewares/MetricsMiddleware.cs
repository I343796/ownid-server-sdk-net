using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Json;
using OwnID.Extensibility.Metrics;
using OwnID.Services;

namespace OwnID.Web.Middlewares
{
    public class MetricsMiddleware : IOwnIDMiddleware
    {
        private static readonly Regex ContextRegex = new(@"ownid\/(?<context>[^\/]*)\/.*", RegexOptions.Compiled);
        private readonly ICacheItemRepository _cacheItemRepository;
        private readonly IEventsMetricsService _eventsMetricsService;
        private readonly ILogger<MetricsMiddleware> _logger;

        private readonly RequestDelegate _next;

        public MetricsMiddleware(ICacheItemRepository cacheItemRepository,
            IEventsMetricsService eventsMetricsService, ILogger<MetricsMiddleware> logger, RequestDelegate next = null)
        {
            _next = next;
            _cacheItemRepository = cacheItemRepository;
            _eventsMetricsService = eventsMetricsService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            // Process request
            httpContext.Request.EnableBuffering();
            try
            {
                await ProcessRequestAsync(httpContext);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
            }

            httpContext.Request.Body.Position = 0;

            var originalBodyStream = httpContext.Response.Body;
            await using var responseBody = new MemoryStream();
            httpContext.Response.Body = responseBody;

            await _next(httpContext);

            // Process response
            try
            {
                await ProcessResponseAsync(httpContext);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
            }

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }

        private string GetOwnIdContext(HttpContext httpContext)
        {
            var match = ContextRegex.Match(httpContext.Request.Path.Value);
            return match.Success ? match.Groups["context"].Value : null;
        }

        private async Task ProcessRequestAsync(HttpContext httpContext)
        {
            if (!httpContext.Request.Path.HasValue
                || httpContext.Request.Method != HttpMethod.Post.Method
                || !httpContext.Request.Path.Value.EndsWith("/start/state")
            )
                return;

            // Do not log if this is second call of "start/state" endpoint
            //
            // TODO: consider rewriting in the way to not use the same endpoint for different actions
            //
            httpContext.Request.EnableBuffering();
            var request = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            if (request.Contains("\"credId\":\""))
            {
                httpContext.Request.Body.Position = 0;
                return;
            }

            httpContext.Request.Body.Position = 0;

            var context = GetOwnIdContext(httpContext);
            if (string.IsNullOrEmpty(context))
                return;

            var item = await _cacheItemRepository.GetAsync(context, false);
            if (item == null)
                return;

            await _eventsMetricsService.LogStartAsync(item.ChallengeType.ToEventType());
        }

        private async Task ProcessResponseAsync(HttpContext httpContext)
        {
            if (!httpContext.Request.Path.HasValue
                || httpContext.Request.Method != HttpMethod.Post.Method
                ||
                !httpContext.Request.Path.Value.EndsWith("/status")
                && !httpContext.Request.Path.Value.EndsWith("/approval-status")
            )
                return;

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var response = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();

            if (httpContext.Request.Path.Value.EndsWith("/status"))
                await ProcessStatusResponseAsync(response);
            else if (httpContext.Request.Path.Value.EndsWith("/approval-status"))
                await ProcessApprovalStatusResponseAsync(response, httpContext);
        }

        private async Task ProcessApprovalStatusResponseAsync(string response, HttpContext httpContext)
        {
            var approvalStatus = OwnIdSerializer.Deserialize<ApprovalStatus>(response);
            if (!string.Equals(approvalStatus.Status, CacheItemStatus.Declined.ToString(),
                StringComparison.InvariantCultureIgnoreCase))
                return;

            var ownIdContext = GetOwnIdContext(httpContext);

            var cacheItem = await _cacheItemRepository.GetAsync(ownIdContext, false);
            if (cacheItem == null)
                return;

            await _eventsMetricsService.LogCancelAsync(cacheItem.ChallengeType.ToEventType());
        }

        private async Task ProcessStatusResponseAsync(string response)
        {
            var statuses = OwnIdSerializer.Deserialize<List<ContextStatus>>(response);
            foreach (var item in statuses.Where(i => i.Status == CacheItemStatus.Finished))
            {
                var cacheItem = await _cacheItemRepository.GetAsync(item.Context, false);
                if (cacheItem == null)
                    continue;

                if (string.IsNullOrEmpty(item.Payload?.Error))
                    await _eventsMetricsService.LogFinishAsync(cacheItem.ChallengeType.ToEventType());
                else
                    await _eventsMetricsService.LogErrorAsync(cacheItem.ChallengeType.ToEventType());
            }
        }

        private class ContextStatus
        {
            public CacheItemStatus Status { get; set; }
            public string Context { get; set; }

            public StatusPayload Payload { get; set; }

            public class StatusPayload
            {
                public string Error { get; set; }
            }
        }

        private class ApprovalStatus
        {
            public string Status { get; set; }
        }
    }
}