using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Json;

namespace OwnID.Cryptography
{
    public class JwtService : IJwtService
    {
        private readonly IOwnIdCoreConfiguration _ownIdCoreConfiguration;

        public JwtService(IOwnIdCoreConfiguration ownIdCoreConfiguration)
        {
            _ownIdCoreConfiguration = ownIdCoreConfiguration;
        }

        /// <summary>
        ///     Decodes provided by OwnId application JWT with data
        /// </summary>
        /// <param name="jwt">Base64 JWT string </param>
        /// <typeparam name="TData">Data type used as model for deserialization</typeparam>
        /// <returns>Context(challenge unique identifier) with <see cref="UserProfileData" /></returns>
        /// <exception cref="Exception">If something went wrong during token validation</exception>
        public (string Context, TData Data) GetDataFromJwt<TData>(string jwt) where TData : ISignedData
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(jwt)) throw new Exception("invalid jwt");

            var token = tokenHandler.ReadJwtToken(jwt);
            var options = OwnIdSerializer.GetDefaultProperties();
            options.PropertyNamingPolicy = null;
            var data = OwnIdSerializer.Deserialize<TData>(token.Payload["data"].ToString(), options);

            using var sr = new StringReader(data.PublicKey);
            var rsaSecurityKey = new RsaSecurityKey(RsaHelper.LoadKeys(sr));

            try
            {
                tokenHandler.ValidateToken(jwt, new TokenValidationParameters
                {
                    IssuerSigningKey = rsaSecurityKey,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    ValidateIssuer = false
                    // TODO: add issuer to token for validation
                }, out _);
            }
            catch (SecurityTokenValidationException ex)
            {
                throw new Exception($"Token failed validation: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                throw new Exception($"Token was invalid: {ex.Message}");
            }

            return (token.Id, data);
        }

        public string GenerateDataJwt(Dictionary<string, object> data, RSA JwtSignCredentials, DateTime? issuedAt = null, Boolean? addKid = false)
        {

            var rsaSecurityKey = new RsaSecurityKey(JwtSignCredentials);

            if (addKid == true)
            {
                rsaSecurityKey.KeyId = Base64UrlEncoder.Encode(rsaSecurityKey.ComputeJwkThumbprint());
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            var expires =
                (issuedAt ?? DateTime.UtcNow).Add(
                    TimeSpan.FromMilliseconds(_ownIdCoreConfiguration.JwtExpirationTimeout));

            issuedAt ??= DateTime.UtcNow.Add(TimeSpan.FromHours(-1));

            var payload = new JwtPayload(null, null, null, issuedAt, expires, issuedAt);

            foreach (var (key, value) in data) payload.Add(key, value);


            var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory
                {
                    CacheSignatureProviders = false
                }
            };

            var jwt = new JwtSecurityToken(new JwtHeader(signingCredentials), payload);

            return tokenHandler.WriteToken(jwt);
        }
        public string GenerateDataJwt(Dictionary<string, object> data, DateTime? issuedAt = null, Boolean? addKid = false)
        {
            return this.GenerateDataJwt(data, _ownIdCoreConfiguration.JwtSignCredentials, issuedAt, addKid);
        }

        /// <summary>
        ///     Gets hash of Base64 encoded JWT string
        /// </summary>
        /// <param name="jwt">Base64 encoded JWT string</param>
        /// <returns>SHA1 Base64 encoded string</returns>
        public string GetJwtHash(string jwt)
        {
            using var sha1 = new SHA1Managed();

            var b64 = Encoding.UTF8.GetBytes(jwt);
            var hash = sha1.ComputeHash(b64);

            return Convert.ToBase64String(hash);
        }
    }
}