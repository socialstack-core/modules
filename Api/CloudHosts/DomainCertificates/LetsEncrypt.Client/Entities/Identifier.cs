using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class Identifier
    {
        /// <summary>
        /// </summary>
        [JsonProperty("type")]
        public IdentifierType Type { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IdentifierType
    {
        /// <summary>
        /// </summary>
        [EnumMember(Value = "dns")]
        Dns
    }
}