using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using OwnID.Extensibility.Flow.Contracts.Jwt;

namespace OwnID.Cryptography
{
    public interface IJwtService
    {
        /// <summary>
        ///     Decodes provided by OwnId application JWT with data
        /// </summary>
        /// <param name="jwt">Base64 JWT string </param>
        /// <typeparam name="TData">Data type used as model for deserialization</typeparam>
        /// <returns>Context(challenge unique identifier) with <see cref="UserProfileData" /></returns>
        /// <exception cref="Exception">If something went wrong during token validation</exception>
        (string Context, TData Data) GetDataFromJwt<TData>(string jwt) where TData : ISignedData;

        string GenerateDataJwt(Dictionary<string, object> data, DateTime? issuedAt = null, Boolean? addKid = false);
        public string GenerateDataJwt(Dictionary<string, object> data, RSA rsaSecurityKey, DateTime? issuedAt = null, Boolean? addKid = false);

        /// <summary>
        ///     Gets hash of Base64 encoded JWT string
        /// </summary>
        /// <param name="jwt">Base64 encoded JWT string</param>
        /// <returns>SHA1 Base64 encoded string</returns>
        string GetJwtHash(string jwt);
    }
}