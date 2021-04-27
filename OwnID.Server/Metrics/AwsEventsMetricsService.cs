using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        ///     Maximum number of Metrics Datum elements sent within one call
        /// </summary>
        /// <remarks>
        ///     From AWS Documentation:
        ///     <code>MetricData.member.N</code>
        ///     The data for the metric. The array can include no more than 20 metrics per call.
        ///     https://docs.aws.amazon.com/AmazonCloudWatch/latest/APIReference/API_PutMetricData.html
        /// </remarks>
        private const int MaximumNumberOfDataForMetric = 20;

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

        private readonly ConcurrentQueue<MetricDatum> _customMetrics = new();

        private readonly ConcurrentQueue<Metric> _loggedData = new();

        private bool ShouldProcess =>
            (!_loggedData.IsEmpty || !_customMetrics.IsEmpty)
            && (_loggedData.Count > _metricsConfiguration.EventsThreshold
                || DateTime.UtcNow - _lastUpdatedTime >= _updateInterval);

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

                    //
                    // TODO: Optimize data send to the AWS by grouping
                    //
                    process = true;
                    while (process)
                    {
                        var batch = new List<MetricDatum>(_metricsConfiguration.EventsThreshold);
                        for (var i = 0; i < MaximumNumberOfDataForMetric; i++)
                        {
                            if (!_customMetrics.TryDequeue(out var awsMetricDatum))
                            {
                                process = false;
                                break;
                            }

                            batch.Add(awsMetricDatum);
                        }

                        _logger.LogDebug("Sending '{BatchCount}' custom metrics events to AWS", batch.Count);

                        sendBatchTasks.Add(SendDataToAwsAsync(batch));
                    }

                    Task.WaitAll(sendBatchTasks.ToArray());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing AWS metrics");
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
            if (metrics == null || metrics.Count <= 0)
                return;
            
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

        public Task LogTimeAsync(string name, TimeSpan duration)
        {
            _customMetrics.Enqueue(new MetricDatum
            {
                MetricName = name,
                TimestampUtc = DateTime.UtcNow,
                Unit = StandardUnit.Milliseconds,
                Value = duration.TotalMilliseconds
            });

            return Task.CompletedTask;
        }

        public IDisposable MeasureTime(string name)
        {
            return new TimeLogger(duration => { LogTimeAsync(name, duration).Wait(); });
        }

        private class TimeLogger : IDisposable
        {
            private readonly Action<TimeSpan> _action;
            private readonly Stopwatch _stopwatch;

            public TimeLogger(Action<TimeSpan> action)
            {
                _action = action;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _action(_stopwatch.Elapsed);
            }
        }


        private Task LogAsync(string metricName)
        {
            _loggedData.Enqueue(new Metric(RoundToMinute(DateTime.UtcNow), metricName));
            return Task.CompletedTask;
        }
    }
}