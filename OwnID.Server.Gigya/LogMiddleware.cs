using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Json;
using OwnID.Extensibility.Logs;
using Serilog.Context;
using Serilog.Core.Enrichers;

namespace OwnID.Server.Gigya
{
    public class LogMiddleware
    {
        private readonly ILogger<LogMiddleware> _logger;
        private readonly RequestDelegate _next;

        public LogMiddleware(RequestDelegate next, ILogger<LogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using var reader = new StreamReader(context.Request.Body);
            var bodyString = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(bodyString))
                return;

            var logMessage = OwnIdSerializer.Deserialize<LogMessage>(bodyString);

            using (LogContext.Push(new PropertyEnricher("source", logMessage.Source ?? "client-side")))
            using (LogContext.Push(new PropertyEnricher("version", logMessage.Version)))
            {
                if (!Enum.TryParse(logMessage.LogLevel, true, out LogLevel logLevel))
                    logLevel = LogLevel.Debug;

                using (_logger.BeginScope($"context: {context}", logMessage.Context))
                {
                    _logger.LogWithData(logLevel, logMessage.Message, logMessage.Data);
                }
            }
        }
    }

    public class LogMessage
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public object Data { get; set; }

        [JsonPropertyName("logLevel")]
        public string LogLevel { get; set; }

        [JsonPropertyName("context")]
        public string Context { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
        
        [JsonPropertyName("source")]
        public string Source { get; set; }
    }
}