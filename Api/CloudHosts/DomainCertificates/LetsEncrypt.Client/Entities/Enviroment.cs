using System;

namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class ApiEnvironment
    {
        /// <summary>
        /// </summary>
        public static Uri LetsEncryptV2 { get; } = new Uri("https://acme-v02.api.letsencrypt.org/directory");

        /// <summary>
        /// </summary>
        public static Uri LetsEncryptV2Staging { get; } = new Uri("https://acme-staging-v02.api.letsencrypt.org/directory");
    }
}