namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class AccountPersisted
    {
        /// <summary>
        /// </summary>
        public string AccountContactEmail { get; set; }

        /// <summary>
        /// </summary>
        public string AccountLocation { get; set; }

        /// <summary>
        /// </summary>
        public string PrivateKeyPem { get; set; }

        /// <summary>
        /// </summary>
        public string PublicKeyPem { get; set; }
    }
}