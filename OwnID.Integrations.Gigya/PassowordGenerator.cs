using System;
using System.Security.Cryptography;

namespace OwnID.Integrations.Gigya
{
    public static class PasswordGenerator
    {
        private const int DefaultPasswordLength = 12;
        private const string PasswordCharsLowerCase = "abcdefghijklmnopqrstuvwxyz";
        private const string PasswordCharsUpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string PasswordCharsNumeric = "0123456789";
        private const string PasswordCharsSpecial = "@$%*&^-+!#_=";

        public static string Generate(int length = DefaultPasswordLength)
        {
            if (length <= 0)
                return null;

            char[][] charGroups =
            {
                PasswordCharsLowerCase.ToCharArray(),
                PasswordCharsUpperCase.ToCharArray(),
                PasswordCharsNumeric.ToCharArray(),
                PasswordCharsSpecial.ToCharArray()
            };

            var charsLeftInGroup = new int[charGroups.Length];

            for (var i = 0; i < charsLeftInGroup.Length; i++)
                charsLeftInGroup[i] = charGroups[i].Length;

            var leftGroupsOrder = new int[charGroups.Length];

            for (var i = 0; i < leftGroupsOrder.Length; i++)
                leftGroupsOrder[i] = i;

            var randomBytes = new byte[4];

            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(randomBytes);
            var seed = BitConverter.ToInt32(randomBytes, 0);
            var random = new Random(seed);

            var password = new char[length];
            var lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;

            for (var i = 0; i < password.Length; i++)
            {
                var nextLeftGroupsOrderIdx = lastLeftGroupsOrderIdx == 0 ? 0 : random.Next(0, lastLeftGroupsOrderIdx);
                var nextGroupIdx = leftGroupsOrder[nextLeftGroupsOrderIdx];
                var lastCharIdx = charsLeftInGroup[nextGroupIdx] - 1;
                var nextCharIdx = lastCharIdx == 0 ? 0 : random.Next(0, lastCharIdx + 1);
                password[i] = charGroups[nextGroupIdx][nextCharIdx];

                if (lastCharIdx == 0)
                {
                    charsLeftInGroup[nextGroupIdx] =
                        charGroups[nextGroupIdx].Length;
                }
                else
                {
                    if (lastCharIdx != nextCharIdx)
                    {
                        var temp = charGroups[nextGroupIdx][lastCharIdx];
                        charGroups[nextGroupIdx][lastCharIdx] =
                            charGroups[nextGroupIdx][nextCharIdx];
                        charGroups[nextGroupIdx][nextCharIdx] = temp;
                    }

                    charsLeftInGroup[nextGroupIdx]--;
                }

                if (lastLeftGroupsOrderIdx == 0)
                {
                    lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;
                }
                else
                {
                    if (lastLeftGroupsOrderIdx != nextLeftGroupsOrderIdx)
                    {
                        var temp = leftGroupsOrder[lastLeftGroupsOrderIdx];
                        leftGroupsOrder[lastLeftGroupsOrderIdx] =
                            leftGroupsOrder[nextLeftGroupsOrderIdx];
                        leftGroupsOrder[nextLeftGroupsOrderIdx] = temp;
                    }

                    lastLeftGroupsOrderIdx--;
                }
            }

            return new string(password);
        }
    }
}