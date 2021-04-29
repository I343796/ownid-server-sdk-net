namespace OwnID.Extensibility.Flow.Contracts.Fido2
{
    public class Fido2Settings
    {
        public string CredId { get; init; }
        
        public string RelyingPartyId { get; init; }

        public string RelyingPartyName { get; init; }

        public string UserDisplayName { get; init; }

        public string UserName { get; init; }
        
        public string CallbackUrl { get; set; }
        
        public string LogLevel { get; set; }
        
        public string LogoUrl { get; set; }
    }
}