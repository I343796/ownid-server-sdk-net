namespace OwnID.Web.Gigya.Configuration
{
    public class GigyaConfiguration : IGigyaConfiguration
    {
        public string DataCenter { get; set; }

        public string ApiKey { get; set; }

        public string UserKey { get; set; }

        public string SecretKey { get; set; }

        public GigyaLoginType LoginType { get; set; }
    }
}