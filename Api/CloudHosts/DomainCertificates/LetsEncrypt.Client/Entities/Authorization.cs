using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class Authorization : BaseEntity
    {
        /// <summary>
        /// </summary>
        [JsonProperty("identifier")]
        public Identifier Identifier { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("status")]
        public AuthorizationStatus? Status { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("expires")]
        public DateTime? Expires { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("scope")]
        public Uri Scope { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("challenges")]
        public IList<Challenge> Challenges { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("wildcard")]
        public bool? Wildcard { get; set; }
    }

    /// <summary>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthorizationStatus
    {
        /// <summary>
        /// </summary>
        [EnumMember(Value = "pending")]
        Pending,

        /// <summary>
        /// </summary>
        [EnumMember(Value = "processing")]
        Processing,

        /// <summary>
        /// </summary>
        [EnumMember(Value = "valid")]
        Valid,

        /// <summary>
        /// </summary>
        [EnumMember(Value = "invalid")]
        Invalid,

        /// <summary>
        /// </summary>
        [EnumMember(Value = "revoked")]
        Revoked,

        /// <summary>
        /// </summary>
        [EnumMember(Value = "deactivated")]
        Deactivated,

        /// <summary>
        /// </summary>
        [EnumMember(Value = "expired")]
        Expired,
    }
}