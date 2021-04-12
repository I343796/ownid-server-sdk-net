using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnID.Extensibility.Metrics;
using OwnID.Web.Middlewares;

namespace OwnID.Server.Metrics
{
    public static class MetricsServiceBuilder
    {
        public static void AddMetrics(this IServiceCollection services, IConfiguration configuration)
        {
            var metricsSection = configuration.GetSection("Metrics");
            var awsSection = configuration.GetSection("AWS");
            var metricsConfig = metricsSection.Get<MetricsConfiguration>();
            var awsConfig = awsSection.Get<AwsConfiguration>();

            if (!metricsConfig?.Enable ?? true)
                return;

            if (string.IsNullOrWhiteSpace(metricsConfig.Namespace))
                throw new InvalidOperationException(
                    $"{nameof(metricsConfig.Namespace)} for metrics config is required");

            if (string.IsNullOrWhiteSpace(awsConfig?.Region) || string.IsNullOrWhiteSpace(awsConfig.AccessKeyId)
                                                             || string.IsNullOrWhiteSpace(awsConfig.SecretAccessKey))
                throw new InvalidOperationException(
                    "Valid AWS config is required for metrics services");
 
            if (metricsConfig.Interval == default)
                metricsConfig.Interval = 60000;

            if (metricsConfig.EventsThreshold == default)
                metricsConfig.EventsThreshold = 100;

            services.TryAddSingleton(awsConfig);
            services.TryAddSingleton(metricsConfig);
            services.TryAddSingleton<IEventsMetricsService, AwsEventsMetricsService>();
        }

        public static void UseMetrics(this IApplicationBuilder app)
        {
            var metricsService = app.ApplicationServices.GetService<IEventsMetricsService>();

            if (metricsService == null)
                return;

            app.UseMiddleware<MetricsMiddleware>();
        }
    }
}