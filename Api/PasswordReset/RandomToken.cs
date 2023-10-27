using System;
using System.Security.Cryptography;
using System.Text;


namespace Api.PasswordResetRequests
{
    /// <summary>
    /// Used for generating random strings.
    /// </summary>
    public static class RandomToken
    {
        private static char[] pattern;
        
        /// <summary>
        /// Generates a crypto safe random token of the given length.
        /// </summary>
        /// <param name="maxSize">The length of the token you'd like.</param>
        /// <returns></returns>
        public static string Generate(int maxSize)
        {
            if(pattern == null)
            {
                pattern = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            }

            return string.Create(maxSize, null, (Span<char> chars, Random r) =>
            {
                Span<byte> randomSpan = stackalloc byte[1];

                for (var i = 0; i < chars.Length; i++)
                {
                    RandomNumberGenerator.Fill(randomSpan);
                    var randomByte = randomSpan[0];
                    chars[i] = pattern[randomByte % pattern.Length];
                }
            });
        }
    }
}
