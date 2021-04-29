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
            var metricsConfig = configuration.GetSection("Metrics").Get<MetricsConfiguration>();
            if (!metricsConfig?.Enable ?? true)
                return;

            var awsConfig = configuration.GetSection("AWS").Get<AwsConfiguration>();
            if (string.IsNullOrWhiteSpace(awsConfig?.Region) || string.IsNullOrWhiteSpace(awsConfig.AccessKeyId)
                                                             || string.IsNullOrWhiteSpace(awsConfig.SecretAccessKey))
                throw new InvalidOperationException(
                    "Valid AWS config is required for metrics services");

            var validator = new MetricsConfigurationValidator();
            validator.FillEmptyWithOptional(metricsConfig);
            validator.Validate(metricsConfig);

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