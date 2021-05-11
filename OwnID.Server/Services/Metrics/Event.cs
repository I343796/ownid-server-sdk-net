using System;

namespace OwnID.Server.Services.Metrics
{
    public readonly struct Event
    {
        public string Namespace { get; }
        public string Name { get; }
        public DateTime Time { get; }

        public Event(string @namespace, string name, DateTime time)
        {
            Namespace = @namespace;
            Name = name;
            Time = time;
        }
    }
}