namespace OwnID.Web.Gigya.Configuration
{
    public interface IGigyaConfiguration
    {
        string DataCenter { get; set; }
        string ApiKey { get; set; }
        string UserKey { get; set; }
        string SecretKey { get; set; }
        GigyaLoginType LoginType { get; set; }
    }
}