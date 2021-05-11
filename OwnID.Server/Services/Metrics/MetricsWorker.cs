using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Logging;

namespace OwnID.Server.Services.Metrics
{
    public class MetricsWorker : IMetricsWorker, IDisposable
    {
        private const int TimerInterval = 5 * 1000;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(5);

        private readonly ILogger<MetricsWorker> _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly AmazonCloudWatchClient _amazonCloudWatchClient;

        private readonly Timer _timer;
        private DateTime _lastUpdatedTime = DateTime.UtcNow;

        private bool ShouldWork =>
            _eventAggregator.EventsCount > 100 || DateTime.UtcNow - _lastUpdatedTime >= _updateInterval;

        public MetricsWorker(ILogger<MetricsWorker> logger, IEventAggregator eventAggregator,
            AwsConfiguration awsConfiguration)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;

            _timer = new Timer(TimerInterval)
            {
                AutoReset = true,
                Enabled = true
            };

            _amazonCloudWatchClient = new AmazonCloudWatchClient(awsConfiguration.AccessKeyId,
                awsConfiguration.SecretAccessKey,
                RegionEndpoint.GetBySystemName(awsConfiguration.Region));
        }

        public void Start()
        {
            _timer.Elapsed += Work;
        }

        private void Work(object sender, ElapsedEventArgs e)
        {
            if (!ShouldWork) return;

            lock (this)
            {
                if (!ShouldWork) return;

                _lastUpdatedTime = DateTime.UtcNow;

                var dataToSend = _eventAggregator.GetDataToSend().ToList();
                if (!dataToSend.Any())
                    return;

                var tasks = dataToSend.Select(SendMetricsToAws);
                Task.WaitAll(tasks.ToArray());
            }
        }

        private async Task SendMetricsToAws(PutMetricDataRequest request)
        {
            try
            {
                _logger.LogInformation("Sending metrics to AWS. Namespace: {Namespace}. Count: {Count}",
                    request.Namespace, request.MetricData.Count);
                await _amazonCloudWatchClient.PutMetricDataAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sending metrics data to the AWS");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}