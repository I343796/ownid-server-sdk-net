using System.Text.Json.Serialization;

namespace OwnID.Integrations.Firebase.Configuration
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