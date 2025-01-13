using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LetsEncrypt.Client.Entities
{
    public partial class Account : BaseEntity
    {
        /// <summary>
        /// </summary>
        [JsonProperty("status")]
        public AccountStatus? Status { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("contact")]
        public List<string> Contact { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("termsOfServiceAgreed")]
        public bool? TermsOfServiceAgreed { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("initialIp")]
        public string InitialIp { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AccountStatus
    {
        /// <summary>
        /// </summary>
        [EnumMember(Value = "valid")]
        Valid,
        /// <summary>
        /// </summary>

        [EnumMember(Value = "deactivated")]
        Deactivated,
        /// <summary>
        /// </summary>

        [EnumMember(Value = "revoked")]
        Revoked,
    }
}