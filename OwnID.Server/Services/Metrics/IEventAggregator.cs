using System;
using System.Collections.Generic;
using Amazon.CloudWatch.Model;

namespace OwnID.Server.Services.Metrics
{
    public interface IEventAggregator
    {
        int EventsCount { get; }
        void AddEvent(string @namespace, string name, DateTime time);
        IList<(Event Event, int Count)> PopAllEvents();
        IEnumerable<PutMetricDataRequest> GetDataToSend();
    }
}