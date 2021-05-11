using System.Threading.Tasks;

namespace OwnID.Extensibility.Metrics
{
    public interface IEventsMetricsService
    {
        Task LogStartAsync(EventType eventType);
        
        Task LogFinishAsync(EventType eventType);
        
        Task LogErrorAsync(EventType eventType);
        
        Task LogSwitchAsync(EventType eventType);
        
        Task LogCancelAsync(EventType eventType);
    }
}