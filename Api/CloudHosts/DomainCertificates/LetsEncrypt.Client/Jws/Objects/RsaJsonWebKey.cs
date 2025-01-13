using Newtonsoft.Json;

namespace LetsEncrypt.Client.Jws
{
    /// <summary>
    /// </summary>
    public class RsaJsonWebKey
    {
        /// <summary>
        /// </summary>
        [JsonProperty("e", Order = 1)]
        public string Exponent { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("kty", Order = 2)]
        public string KeyType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("n", Order = 3)]
        public string Modulus { get; set; }
    }
}