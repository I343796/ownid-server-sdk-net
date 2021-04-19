namespace OwnID.Integrations.Firebase.Contracts
{
    public class RecoveryPayload
    {
        public string OobCode { get; set; }
        
        public string ApiKey { get; set; }
    }
}