using Newtonsoft.Json;

namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class CertificateRevocation
    {
        /// <summary>
        /// </summary>
        [JsonProperty("certificate")]
        public string Certificate { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("reason")]
        public RevocationReason? Reason { get; set; }
    }

    /// <summary>
    /// </summary>
    public enum RevocationReason
    {
        /// <summary>
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// </summary>
        KeyCompromise = 1,
        /// <summary>
        /// </summary>
        CACompromise = 2,
        /// <summary>
        /// </summary>
        AffiliationChanged = 3,
        /// <summary>
        /// </summary>
        Superseded = 4,
        /// <summary>
        /// </summary>
        CessationOfOperation = 5,
        /// <summary>
        /// </summary>
        CertificateHold = 6,
        /// <summary>
        /// </summary>
        RemoveFromCRL = 8,
        /// <summary>
        /// </summary>
        PrivilegeWithdrawn = 9,
        /// <summary>
        /// </summary>
        AACompromise = 10,
    }
}