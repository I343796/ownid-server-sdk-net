using System;
using System.Threading.Tasks;
using OwnID.Extensibility.Metrics;

namespace OwnID.Server.Services.Metrics
{
    public class AwsEventsMetricsService : IEventsMetricsService
    {
        private readonly MetricsConfiguration _metricsConfiguration;
        private readonly IEventAggregator _eventAggregator;

        public AwsEventsMetricsService(MetricsConfiguration metricsConfiguration, IEventAggregator eventAggregator)
        {
            _metricsConfiguration = metricsConfiguration;
            _eventAggregator = eventAggregator;
        }

        public Task LogStartAsync(EventType eventType)
        {
            return LogAsync(eventType.ToString());
        }

        public Task LogFinishAsync(EventType eventType)
        {
            return LogAsync($"{eventType} success");
        }

        public Task LogErrorAsync(EventType eventType)
        {
            return LogAsync($"{eventType} error");
        }

        public Task LogSwitchAsync(EventType eventType)
        {
            return LogAsync($"{eventType} switched");
        }

        public Task LogCancelAsync(EventType eventType)
        {
            return LogAsync($"{eventType} canceled");
        }

        private Task LogAsync(string metricName)
        {
            _eventAggregator.AddEvent(_metricsConfiguration.Namespace, metricName, DateTime.UtcNow);
            return Task.CompletedTask;
        }
    }
}