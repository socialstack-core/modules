using Newtonsoft.Json;

namespace LetsEncrypt.Client.Jws
{
    /// <summary>
    /// </summary>
    public class JwsData
    {
        /// <summary>
        /// </summary>
        [JsonProperty("protected")]
        public string Protected { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("payload")]
        public string Payload { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
}