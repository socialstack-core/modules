using System.Security.Cryptography;

namespace LetsEncrypt.Client.Cryptography
{
    /// <summary>
    /// </summary>
    public class Sha256HashProvider
    {
        /// <summary>
        /// </summary>
        public static byte[] ComputeHash(byte[] data)
        {
            using (var hasher = SHA256.Create())
            {
                return hasher.ComputeHash(data);
                //var hashBytes = hasher.ComputeHash(data);
                //return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
            }
        }
    }
}