using OwnID.Extensibility.Flow.Contracts;

namespace OwnID.Integrations.Firebase.Contracts
{
    internal class Connection : OwnIdConnection
    {
        public string KeyHsh { get; set; }
        
        public string UserId { get; set; }
    }
}