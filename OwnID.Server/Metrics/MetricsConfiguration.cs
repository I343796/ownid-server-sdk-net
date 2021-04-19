namespace OwnID.Server.Metrics
{
    public class MetricsConfiguration
    {
        public bool Enable { get; set; }
        
        public string Namespace { get; set; }
        
        public uint Interval { get; set; }
        
        public int EventsThreshold { get; set; }
    }
}