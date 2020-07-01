using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OwnIdSdk.NetCore3.Web;
using Serilog.Context;
using Serilog.Core.Enrichers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OwnIdSdk.NetCore3.Server.Gigya
{
    public class LogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogRequestMiddleware> _logger;

        public LogMiddleware(RequestDelegate next, ILogger<LogRequestMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using var reader = new StreamReader(context.Request.Body);
            var bodyString = await reader.ReadToEndAsync();
            
            if(string.IsNullOrEmpty(bodyString))
                return;

            using (LogContext.Push(new PropertyEnricher("source", "webapp")))
            {
                var logMessage = JsonSerializer.Deserialize<LogMessage>(bodyString);
                if (!Enum.TryParse(logMessage.LogLevel, true, out LogLevel logLevel))
                {
                    _logger.Log(LogLevel.Warning, "Log with unknown format {logJson}", bodyString);
                    return;
                }

                using (_logger.BeginScope("context: {context}", logMessage.Context))
                    _logger.LogWithData(logLevel, logMessage.Message, logMessage.Data);
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
    }
}