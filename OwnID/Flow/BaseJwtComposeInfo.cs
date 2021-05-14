using System;
using OwnID.Flow.ResultActions;

namespace OwnID.Flow
{
    public class BaseJwtComposeInfo
    {
        public BaseJwtComposeInfo(ITransitionInput input)
        {
            Context = input.Context;
            ClientTime = input.ClientDate;
            Locale = input.CultureInfo?.Name;
            IsDesktop = input.IsDesktop;
        }

        /// <summary>
        ///     User data encryption key
        /// </summary>
        public string EncKey { get; set; }

        /// <summary>
        ///     User data encryption vector
        /// </summary>
        public string EncVector { get; set; }

        /// <summary>
        ///     Response locale
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        ///     Include requester data
        /// </summary>
        public bool IncludeRequester { get; set; }

        /// <summary>
        ///     User browser time
        /// </summary>
        public DateTime? ClientTime { get; set; }

        /// <summary>
        ///     Auth process unique identifier
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        ///     Possible behavior for WebApp
        /// </summary>
        public FrontendBehavior Behavior { get; set; }

        /// <summary>
        ///     Identifies if connection can be recovered
        /// </summary>
        public bool CanBeRecovered { get; set; }

        /// <summary>
        ///     Include FIDO2 fallback behavior configuration
        /// </summary>
        public bool IncludeFido2FallbackBehavior { get; set; }

        /// <summary>
        ///     Is desktop
        /// </summary>
        public bool IsDesktop { get; set; }


        /// <summary>
        ///     Cookies was set
        /// </summary>
        public bool CookiesSet { get; set; }
    }
}