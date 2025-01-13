using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class Challenge : BaseEntity
    {
        /// <summary>
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("status")]
        public ChallengeStatus? Status { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("validated")]
        public DateTime? Validated { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        //

        /// <summary>
        /// </summary>
        [JsonIgnore]
        public string DnsKey { get; set; }

        /// <summary>
        /// </summary>
        [JsonIgnore]
        public string VerificationKey { get; set; }

        /// <summary>
        /// </summary>
        [JsonIgnore]
        public string VerificationValue { get; set; }
    }

    /// <summary>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChallengeStatus
    {
        /// <summary>
        /// </summary>
        [JsonProperty("pending")]
        Pending,

        /// <summary>
        /// </summary>
        [JsonProperty("processing")]
        Processing,

        /// <summary>
        /// </summary>
        [JsonProperty("valid")]
        Valid,

        /// <summary>
        /// </summary>
        [JsonProperty("invalid")]
        Invalid,
    }

    /// <summary>
    /// </summary>
    public static class ChallengeType
    {
        /// <summary>
        /// </summary>
        public const string Http01 = "http-01";

        /// <summary>
        /// </summary>
        public const string Dns01 = "dns-01";

        /// <summary>
        /// </summary>
        public const string TlsAlpn01 = "tls-alpn-01";
    }
}