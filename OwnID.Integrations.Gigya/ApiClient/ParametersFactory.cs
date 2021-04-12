using System.Collections.Generic;
using OwnID.Extensibility.Json;
using OwnID.Integrations.Gigya.Configuration;

namespace OwnID.Integrations.Gigya.ApiClient
{
    internal static class ParametersFactory
    {
        /// <summary>
        ///     Create initial parameters collection with populated auth parameters
        /// </summary>
        /// <param name="configuration">configuration</param>
        /// <returns>parameters collection</returns>
        public static IList<KeyValuePair<string, string>> CreateAuthParameters(IGigyaConfiguration configuration)
        {
            var result = CreateApiKeyParameter(configuration)
                .AddParameter("secret", configuration.SecretKey);

            if (!string.IsNullOrEmpty(configuration.UserKey))
                result.AddParameter("userKey", configuration.UserKey);

            return result;
        }

        /// <summary>
        ///     Create initial parameters collection with populated apiKey parameter
        /// </summary>
        /// <param name="configuration">configuration</param>
        /// <returns>parameters collection</returns>
        public static IList<KeyValuePair<string, string>> CreateApiKeyParameter(IGigyaConfiguration configuration)
        {
            var result = new List<KeyValuePair<string, string>>();

            result.AddParameter("apiKey", configuration.ApiKey);

            return result;
        }

        public static IList<KeyValuePair<string, string>> AddParameter(
            this IList<KeyValuePair<string, string>> nameValueCollection,
            string key,
            string value)
        {
            if (!string.IsNullOrEmpty(value))
                nameValueCollection.Add(new KeyValuePair<string, string>(key, value));

            return nameValueCollection;
        }

        public static IList<KeyValuePair<string, string>> AddParameter<T>(
            this IList<KeyValuePair<string, string>> nameValueCollection,
            string key,
            T value)
        {
            nameValueCollection.Add(
                new KeyValuePair<string, string>(key, OwnIdSerializer.Serialize(value)));

            return nameValueCollection;
        }
    }
}