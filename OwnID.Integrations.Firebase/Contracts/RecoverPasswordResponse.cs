namespace OwnID.Integrations.Firebase.Contracts
{
    public class RecoverPasswordResponse
    {
        public string Email { get; set; }
        
        public string RequestType { get; set; }
        
        public RecoveryError Error { get; set; }
        
        public class RecoveryError
        {
            public string Message { get; set; }
            
            public string Code { get; set; }
        }
    }
}