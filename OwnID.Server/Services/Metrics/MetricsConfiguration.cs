using System;

namespace OwnID.Server.Services.Metrics
{
    public class MetricsConfiguration
    {
        public bool Enable { get; set; }

        //
        // TODO: Move namespace to OwnIdCoreConfiguration?
        //
        public string Namespace { get; set; }

        public uint Interval { get; set; }

        public int EventsThreshold { get; set; }
    }
}