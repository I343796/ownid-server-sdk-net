using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Logs;
using OwnID.Redis;

namespace OwnID.Server.Metrics
{
    public class TelemetryLogService : IHostedService, IDisposable
    {
        private readonly AmazonCloudWatchClient _amazonCloudWatchClient;
        private readonly ILogger _logger;
        private readonly MetricsConfiguration _metricsConfiguration;
        private readonly Process _process = Process.GetCurrentProcess();
        private readonly RedisCacheStore _redisStore;
        private DateTime _lastTimeStamp;
        private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
        private TimeSpan _lastUserProcessorTime = TimeSpan.Zero;
        private Timer _memoryCpuTimer;
        private Timer _redisTimer;

        public TelemetryLogService(ILogger<TelemetryLogService> logger, ICacheStore store,
            AwsConfiguration awsConfiguration = null, MetricsConfiguration metricsConfiguration = null)
        {
            _logger = logger;
            _metricsConfiguration = metricsConfiguration;
            _redisStore = store as RedisCacheStore;

            if (awsConfiguration != null && metricsConfiguration != null)
                _amazonCloudWatchClient = new AmazonCloudWatchClient(awsConfiguration.AccessKeyId,
                    awsConfiguration.SecretAccessKey,
                    RegionEndpoint.GetBySystemName(awsConfiguration.Region));
        }

        public void Dispose()
        {
            _memoryCpuTimer?.Dispose();
            _redisTimer?.Dispose();
            _amazonCloudWatchClient?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _memoryCpuTimer = new Timer(LogTelemetry, null, TimeSpan.Zero,
                    TimeSpan.FromMinutes(3));

                if (_redisStore != null && _amazonCloudWatchClient != null)
                    _redisTimer = new Timer(LogRedisInfo, null, TimeSpan.Zero,
                        TimeSpan.FromMilliseconds(_metricsConfiguration.Interval));
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _memoryCpuTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void LogTelemetry(object state)
        {
            var totalCpuTimeUsed = _process.TotalProcessorTime.TotalMilliseconds -
                                   _lastTotalProcessorTime.TotalMilliseconds;
            var userCpuTimeUsed =
                _process.UserProcessorTime.TotalMilliseconds - _lastUserProcessorTime.TotalMilliseconds;

            _lastTotalProcessorTime = _process.TotalProcessorTime;
            _lastUserProcessorTime = _process.UserProcessorTime;

            var cpuTimeElapsed = (DateTime.UtcNow - _lastTimeStamp).TotalMilliseconds * Environment.ProcessorCount;
            _lastTimeStamp = DateTime.UtcNow;

            _logger.LogWithData(LogLevel.Information, "CPU / RAM telemetry", new
            {
                totalCpu = totalCpuTimeUsed * 100 / cpuTimeElapsed,
                userCpu = userCpuTimeUsed * 100 / cpuTimeElapsed,
                workingSet64 = _process.WorkingSet64,
                nonpagedSystemMemorySize64 = _process.NonpagedSystemMemorySize64,
                pagedMemorySize64 = _process.PagedMemorySize64,
                pagedSystemMemorySize64 = _process.PagedSystemMemorySize64,
                privateMemorySize64 = _process.PrivateMemorySize64,
                virtualMemorySize64 = _process.VirtualMemorySize64
            });
        }

        private async void LogRedisInfo(object state)
        {
            try
            {
                var stats = await _redisStore.GetMemoryStatsAsync();
                var timeStamp = DateTime.UtcNow;
                await _amazonCloudWatchClient.PutMetricDataAsync(new PutMetricDataRequest
                {
                    MetricData = new List<MetricDatum>
                    {
                        new()
                        {
                            Unit = StandardUnit.Count,
                            Value = stats.keysCount,
                            TimestampUtc = timeStamp,
                            MetricName = "Redis.KeysCount"
                        },
                        new()
                        {
                            Unit = StandardUnit.Bytes,
                            Value = stats.itemsSize,
                            TimestampUtc = timeStamp,
                            MetricName = "Redis.KeysSize"
                        }
                    },
                    Namespace = _metricsConfiguration.Namespace
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "LogRedisInfo");
            }
        }
    }
}