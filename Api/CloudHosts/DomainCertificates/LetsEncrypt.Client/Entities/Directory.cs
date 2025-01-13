using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class Directory : BaseEntity
    {
        /// <summary>
        /// </summary>
        [JsonProperty("newNonce")]
        public Uri NewNonce { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("newAccount")]
        public Uri NewAccount { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("newOrder")]
        public Uri NewOrder { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("revokeCert")]
        public Uri RevokeCert { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("keyChange")]
        public Uri KeyChange { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("meta")]
        public DirectoryMeta Meta { get; set; }
    }

    /// <summary>
    /// </summary>
    public class DirectoryMeta
    {
        /// <summary>
        /// </summary>
        [JsonProperty("termsOfService")]
        public Uri TermsOfService { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("website")]
        public Uri Website { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("caaIdentities")]
        public List<string> CaaIdentities { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty("externalAccountRequired")]
        public bool? ExternalAccountRequired { get; set; }
    }
}