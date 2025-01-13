using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class Order : BaseEntity
    {
        /// <summary>
        /// </summary>
        [JsonProperty("status")]
        public OrderStatus? Status { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("expires")]
        public DateTime? Expires { get; set; }

        /// <summary>
        /// </summary>
        public IList<Identifier> Identifiers { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("notBefore")]
        public DateTime? NotBefore { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("notAfter")]
        public DateTime? NotAfter { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("authorizations")]
        public List<Uri> Authorizations { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("finalize")]
        public Uri Finalize { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("certificate")]
        public Uri Certificate { get; set; }
    }

    /// <summary>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderStatus
    {
        /// <summary>
        /// </summary>
        [EnumMember(Value = "pending")]
        Pending,

        /// <summary>
        /// </summary>
        [EnumMember(Value = "ready")]
        Ready,

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
    }

    /// <summary>
    /// </summary>
    public class OrderCertificate : Order
    {
        /// <summary>
        /// </summary>
        [JsonProperty("csr")]
        public string Csr { get; set; }
    }
}