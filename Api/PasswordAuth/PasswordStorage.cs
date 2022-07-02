using System;
using System.Security.Cryptography;
using System.Text;


namespace Api.PasswordAuth
{
    /// <summary>
    /// Wordpress compatible password storage via Phpass.
    /// </summary>
    class PasswordStorage
    {
        /// <summary>
        /// Creates a Wordpress compatible hash.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string CreateHash(string password)
        {
            var crypt = new CryptSharp.PhpassCrypter();
            var salt = crypt.GenerateSalt(new CryptSharp.CrypterOptions()
        {
            { CryptSharp.CrypterOption.Rounds, 8 }
            });
            
            return crypt.Crypt(password, salt);
        }

        /// <summary>
        /// Verifies a Wordpress compatible hash.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="goodHash"></param>
        /// <returns></returns>
        public static bool VerifyPassword(string password, string goodHash)
        {
            var crypt = new CryptSharp.PhpassCrypter();
            var res = crypt.Crypt(password, goodHash);
            return res == goodHash;
        }
        
    }

}