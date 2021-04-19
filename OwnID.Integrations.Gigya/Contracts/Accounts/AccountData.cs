using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using OwnID.Extensibility.Flow.Contracts;

namespace OwnID.Integrations.Gigya.Contracts.Accounts
{
    /// <summary>
    ///     Account data save at Gigya profile
    /// </summary>
    /// <remarks>
    ///     Being passed as a <code>data</code> parameter during calling accounts.setAccountInfo method
    ///     https://developers.gigya.com/display/GD/accounts.setAccountInfo+REST
    /// </remarks>
    public class AccountData
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public AccountData()
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="connection">OwnID Connection Gigya connection data</param>
        public AccountData([NotNull] GigyaOwnIdConnection connection)
        {
            OwnId.Connections = new List<GigyaOwnIdConnection>(1)
            {
                connection
            };
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="pubKeys">public keys</param>
        public AccountData(IEnumerable<string> pubKeys)
        {
            OwnId.Connections = pubKeys.Distinct().Select(x => new GigyaOwnIdConnection {PublicKey = x}).ToList();
        }

        /// <summary>
        ///     Represents OwnID data
        /// </summary>
        [JsonPropertyName("ownId")]
        public OwnIdData OwnId { get; set; } = new();
    }

    public class OwnIdData
    {
        /// <summary>
        ///     Contains public keys linked to account
        /// </summary>
        [JsonPropertyName("connections")]
        public List<GigyaOwnIdConnection> Connections { get; set; } = new(0);
        
        /// <summary>
        ///     Contains user settings if any defined
        /// </summary>
        [JsonPropertyName("settings")]
        public UserSettings UserSettings { get; set; }
    }
}