using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace OwnID.Server.Services.Metrics
{
    public class EventAggregator : IEventAggregator
    {
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

        private readonly ConcurrentDictionary<Event, int> _events = new();


        public int EventsCount
        {
            get { return _events.Sum(x => x.Value); }
        }

        public void AddEvent(string @namespace, string name, DateTime time)
        {
            _events.AddOrUpdate(new Event(@namespace, name, time), 1, (_, i) => i + 1);
        }

        public IList<(Event Event, int Count)> PopAllEvents()
        {
            var result = new List<(Event Event, int Count)>();

            foreach (var key in _events.Keys)
            {
                if (!_events.TryRemove(key, out var count))
                    continue;

                result.Add((key, count));
            }

            return result;
        }

        public IEnumerable<PutMetricDataRequest> GetDataToSend()
        {
            var eventsToSend = PopAllEvents();

            var namespaces = eventsToSend.GroupBy(x => x.Event.Namespace);
            foreach (var group in namespaces)
            {
                //group.Key
                var allEventsInNamespace = group.Select(x => new MetricDatum()
                {
                    MetricName = x.Event.Name,
                    TimestampUtc = x.Event.Time,
                    Value = x.Count,
                    Unit = StandardUnit.Count
                }).ToList();

                foreach (var itemsToSend in SplitList(allEventsInNamespace, MaximumNumberOfDataForMetric))
                {
                    yield return new PutMetricDataRequest
                    {
                        Namespace = group.Key,
                        MetricData = itemsToSend,
                    };
                }
            }
        }

        private static IEnumerable<List<T>> SplitList<T>(List<T> source, int size)
        {
            for (var i = 0; i < source.Count; i += size)
            {
                yield return source.GetRange(i, Math.Min(size, source.Count - i));
            }
        }
    }
}