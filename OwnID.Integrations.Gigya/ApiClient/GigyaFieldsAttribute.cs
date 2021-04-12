using System;

namespace OwnID.Integrations.Gigya.ApiClient
{
    /// <summary>
    ///     Attribute which describe which fields need to be queries from the Gigya
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GigyaFieldsAttribute : Attribute
    {
        /// <summary>
        ///     Coma separated list of fields to get
        /// </summary>
        public string Fields { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="fields">Coma separated list of fields to get</param>
        public GigyaFieldsAttribute(string fields)
        {
            Fields = fields;
        }
    }
}