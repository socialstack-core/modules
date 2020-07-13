using System.Security.Cryptography;
using System.Text;


namespace Api.PasswordResetRequests
{
	/// <summary>
	/// Used for generating random strings.
	/// </summary>
    public static class RandomToken
    {
        /// <summary>
        /// Generates a crypto safe random token of the given length.
        /// </summary>
        /// <param name="maxSize">The length of the token you'd like.</param>
        /// <returns></returns>
        public static string Generate(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }
    }
}
