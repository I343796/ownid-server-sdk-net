using System;
using System.Diagnostics;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Contracts.Cookies;
using OwnID.Extensibility.Flow.Contracts.Start;

namespace OwnID.Extensibility.Cache
{
    /// <summary>
    ///     OwnID flow detection fields store structure
    /// </summary>
    [DebuggerDisplay("{Context}: {Status} (ChallengeType: {ChallengeType}/FlowType: {FlowType})")]
    public class CacheItem : ICloneable
    {
        /// <summary>
        ///     OwnID flow unique identifier
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        ///     State
        /// </summary>
        public CacheItemStatus Status { get; set; } = CacheItemStatus.Initiated;

        /// <summary>
        ///     Nonce
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        ///     User unique identity
        /// </summary>
        public string DID { get; set; }

        /// <summary>
        ///     Challenge type related to the <c>Context</c> and <see cref="Nonce" />
        /// </summary>
        public ChallengeType ChallengeType { get; set; }

        /// <summary>
        ///     <see cref="Flow.FlowType" /> that should be used for current OwnID flow
        /// </summary>
        public FlowType FlowType { get; set; }

        /// <summary>
        ///     Indicate if current flow is FIDO2 flow
        /// </summary>
        public bool IsFido2Flow =>
            FlowType == FlowType.Fido2Login
            || FlowType == FlowType.Fido2Register
            || FlowType == FlowType.Fido2Link
            || FlowType == FlowType.Fido2LinkWithPin
            || FlowType == FlowType.Fido2Recover
            || FlowType == FlowType.Fido2RecoverWithPin;

        /// <summary>
        ///     Request Token from Web App
        /// </summary>
        public string RequestToken { get; set; }

        /// <summary>
        ///     Request Token to send to Web App
        /// </summary>
        public string ResponseToken { get; set; }

        /// <summary>
        ///     Payload
        /// </summary>
        /// <remarks>
        ///     General purpose payload to be used by integrated providers for theirs needs
        ///     (for example, account recover payload can store password reset token)
        /// </remarks>
        public string Payload { get; set; }

        /// <summary>
        ///     Prevents multiple concurrent assignments to current instance of <see cref="CacheItem" />
        /// </summary>
        public string ConcurrentId { get; set; }

        /// <summary>
        ///     Used for security checks (PIN and etc.)
        /// </summary>
        public string SecurityCode { get; set; }

        /// <summary>
        ///     Stores public key for partial auth flow
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        ///     Indicates if cache item can be used in middle of register / login process
        /// </summary>
        public bool IsValidForAuthorize => !HasFinalState &&
                                           (ChallengeType == ChallengeType.Register ||
                                            ChallengeType == ChallengeType.Login);

        /// <summary>
        ///     Indicates if cache item can be used in middle of link process
        /// </summary>
        public bool IsValidForLink => !HasFinalState && ChallengeType == ChallengeType.Link;

        /// <summary>
        ///     Indicates if cache item can be used in middle of recover process
        /// </summary>
        public bool IsValidForRecover => !HasFinalState && ChallengeType == ChallengeType.Recover;

        /// <summary>
        ///     Indicated if cache item has final state and status can not be changed
        /// </summary>
        public bool HasFinalState => Status == CacheItemStatus.Finished || Status == CacheItemStatus.Declined;

        /// <summary>
        ///     Fido2 counter
        /// </summary>
        public uint? Fido2SignatureCounter { get; set; }

        /// <summary>
        ///     Fido2 credential id
        /// </summary>
        public string Fido2CredentialId { get; set; }

        /// <summary>
        ///     Error
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        ///     Recovery Token
        /// </summary>
        public string RecoveryToken { get; set; }

        /// <summary>
        ///     Connection recovery data
        /// </summary>
        public string RecoveryData { get; set; }

        /// <summary>
        ///     Cookie type
        /// </summary>
        public CookieType AuthCookieType { get; set; }

        /// <summary>
        ///     Private key encryption passphrase
        /// </summary>
        public string EncKey { get; set; }
        
        /// <summary>
        ///     Private key encryption vector
        /// </summary>
        public string EncVector { get; set; }

        public ChallengeType InitialChallengeType { get; set; }
        
        /// <summary>
        ///     New auth type
        /// </summary>
        /// <remarks>Used during connection upgrade process (for example, from basic to fido2)</remarks>
        public ConnectionAuthType NewAuthType { get; set; }
        
        /// <summary>
        ///     Old public key
        /// </summary>
        /// <remarks>Used during connection upgrade process (for example, from basic to fido2)</remarks>
        public string OldPublicKey { get; set; }
        
        public bool IsDesktop { get; set; }

        /// <summary>
        ///     Creates new instance of <see cref="CacheItem" /> based on <see cref="Nonce" /> and <see cref="DID" />
        /// </summary>
        public object Clone()
        {
            return new CacheItem
            {
                DID = DID,
                Nonce = Nonce,
                ChallengeType = ChallengeType,
                InitialChallengeType = InitialChallengeType,
                FlowType = FlowType,
                Status = Status,
                RequestToken = RequestToken,
                ResponseToken = ResponseToken,
                Context = Context,
                Payload = Payload,
                ConcurrentId = ConcurrentId,
                SecurityCode = SecurityCode,
                PublicKey = PublicKey,
                Fido2SignatureCounter = Fido2SignatureCounter,
                Fido2CredentialId = Fido2CredentialId,
                Error = Error,
                RecoveryData = RecoveryData,
                RecoveryToken = RecoveryToken,
                EncKey = EncKey,
                EncVector = EncVector,
                AuthCookieType = AuthCookieType,
                IsDesktop = IsDesktop,
                NewAuthType = NewAuthType,
                OldPublicKey = OldPublicKey
            };
        }

        /// <summary>
        ///     Get CacheItem auth type string representation
        /// </summary>
        /// <returns>auth type string representation</returns>
        public string GetAuthType()
        {
            if (FlowType == FlowType.Fido2Login
                || FlowType == FlowType.Fido2Register
                || FlowType == FlowType.Fido2Link
                || FlowType == FlowType.Fido2LinkWithPin
                || FlowType == FlowType.Fido2Recover
                || FlowType == FlowType.Fido2RecoverWithPin)
                return "FIDO2";

            return AuthCookieType == CookieType.Passcode? "Passcode" : "Basic";
        }
    }
}