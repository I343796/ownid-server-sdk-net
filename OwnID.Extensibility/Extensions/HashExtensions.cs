using System;
using System.Security.Cryptography;
using System.Text;

namespace OwnID.Extensibility.Extensions
{
    public static class HashExtensions
    {
        public static string ToSha256(this string input)
        {
            using var sha256 = new SHA256Managed();
            var hash = Convert.ToBase64String(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(input)));

            return hash;
        }
    }
}