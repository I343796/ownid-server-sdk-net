using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Metrics;

namespace OwnID.Server.Metrics
{
    public class AwsEventsMetricsService : IEventsMetricsService, IDisposable
    {
        private const int TimerInterval = 5 * 1000;
        
        private readonly struct Metric
        {
            public DateTime Date { get; }
            public string Name { get; }

            public Metric(DateTime date, string name)
            {
                Date = date;
                Name = name;
            }
        }

        private readonly MetricsConfiguration _metricsConfiguration;
        private readonly ILogger<AwsEventsMetricsService> _logger;

        private readonly TimeSpan _updateInterval;
        private readonly Timer _timer;
        private DateTime _lastUpdatedTime = DateTime.UtcNow;
        private readonly AmazonCloudWatchClient _amazonCloudWatchClient;

        private readonly ConcurrentQueue<Metric> _loggedData = new ConcurrentQueue<Metric>();

        private bool ShouldProcess =>
            _loggedData.Count != 0
            && (_loggedData.Count > _metricsConfiguration.EventsThreshold || DateTime.UtcNow - _lastUpdatedTime >= _updateInterval);

        public AwsEventsMetricsService(AwsConfiguration awsConfiguration, MetricsConfiguration metricsConfiguration,
            ILogger<AwsEventsMetricsService> logger)
        {
            _metricsConfiguration = metricsConfiguration;
            _updateInterval = TimeSpan.FromMilliseconds(metricsConfiguration.Interval);
            _logger = logger;

            _timer = new Timer(TimerInterval)
            {
                AutoReset = true,
                Enabled = true
            };
            _timer.Elapsed += ProcessQueue;

            _amazonCloudWatchClient = new AmazonCloudWatchClient(awsConfiguration.AccessKeyId,
                awsConfiguration.SecretAccessKey,
                RegionEndpoint.GetBySystemName(awsConfiguration.Region));
        }

        private readonly object _processQueueSync = new object();

        private void ProcessQueue(object sender, ElapsedEventArgs e)
        {
            if (!ShouldProcess)
                return;

            lock (_processQueueSync)
            {
                if (!ShouldProcess)
                    return;

                try
                {
                    _lastUpdatedTime = DateTime.UtcNow;

                    var sendBatchTasks = new List<Task>();
                    var process = true;
                    while (process)
                    {
                        var items = new List<Metric>(_metricsConfiguration.EventsThreshold);
                        for (var i = 0; i < _metricsConfiguration.EventsThreshold; i++)
                        {
                            if (!_loggedData.TryDequeue(out var item))
                            {
                                process = false;
                                break;
                            }

                            items.Add(item);
                        }

                        sendBatchTasks.Add(SendBatchAsync(items));
                    }

                    Task.WaitAll(sendBatchTasks.ToArray());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }

        private async Task SendBatchAsync(IList<Metric> items)
        {
            if (!items.Any())
                return;

            var metrics = new Dictionary<Metric, int>();
            foreach (var item in items)
            {
                if (!metrics.ContainsKey(item))
                    metrics.Add(item, 0);

                metrics[item]++;
            }

            var awsMetrics = metrics.Keys.Select(key => new MetricDatum
            {
                MetricName = key.Name,
                TimestampUtc = key.Date,
                Unit = StandardUnit.Count,
                Value = metrics[key]
            }).ToList();

            await SendDataToAwsAsync(awsMetrics);
        }

        private async Task SendDataToAwsAsync(List<MetricDatum> metrics)
        {
            var request = new PutMetricDataRequest
            {
                MetricData = metrics,
                Namespace = _metricsConfiguration.Namespace
            };

            try
            {
                await _amazonCloudWatchClient.PutMetricDataAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private static DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        private static DateTime RoundToMinute(DateTime dt)
        {
            return RoundUp(dt, TimeSpan.FromMinutes(1));
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _amazonCloudWatchClient?.Dispose();
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
            _loggedData.Enqueue(new Metric(RoundToMinute(DateTime.UtcNow), metricName));
            return Task.CompletedTask;
        }
    }
}