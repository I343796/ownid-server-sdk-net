using System.Text.Json.Serialization;

namespace OwnID.Integrations.Firebase.Contracts
{
    public interface IFirebaseConfiguration
    {
        string CredentialsJson { get; set; }
    }

    public class FirebaseServiceCredentials
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }
    }
}