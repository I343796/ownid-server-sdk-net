using System;
using OwnID.Extensibility.Configuration.Validators;

namespace OwnID.Server.Services.Metrics
{
    public class MetricsConfigurationValidator : IConfigurationValidator<MetricsConfiguration>
    {
        public void FillEmptyWithOptional(MetricsConfiguration configuration)
        {
            if (configuration.Interval == default)
                configuration.Interval = 60000;

            if (configuration.EventsThreshold == default)
                configuration.EventsThreshold = 100;
        }

        public void Validate(MetricsConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.Namespace))
                throw new InvalidOperationException(
                    $"{nameof(configuration.Namespace)} for metrics config is required");
        }
    }
}