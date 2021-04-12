using System.Diagnostics;

namespace OwnID.Integrations.Gigya.Contracts
{
    /// <summary>
    ///     Gigya validation error
    /// </summary>
    /// <remarks>
    ///     Part of Gigya error response ("validationErrors" field)
    /// </remarks>
    /// <example>
    ///     Validation error example:
    ///     <code>
    ///         {
    ///            "errorCode": 400006,
    ///            "message": "Unallowed value for field: email",
    ///            "fieldName": "profile.email"
    ///         }
    ///     </code>
    /// </example>
    [DebuggerDisplay("{" + nameof(FieldName) + "}: {" + nameof(ErrorCode) + "} - {" + nameof(Message) + "}")]
    public class GigyaValidationError
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public string FieldName { get; set; }
    }
}