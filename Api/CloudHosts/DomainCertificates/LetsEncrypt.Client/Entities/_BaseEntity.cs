using Newtonsoft.Json;
using System;
using System.Net;

namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class BaseEntity
    {
        /// <summary>
        /// </summary>
        [JsonIgnore]
        public virtual string UnknownContent { get; set; }

        /// <summary>
        /// </summary>
        [JsonIgnore]
        public virtual Uri Location { get; set; }

        /// <summary>
        /// </summary>
        [JsonIgnore]
        public virtual AcmeError Error { get; set; }
    }

    /// <summary>
    /// </summary>
    public class AcmeError
    {
        /// <summary>
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// </summary>
        public string Detail { get; set; }
        /// <summary>
        /// </summary>
        public HttpStatusCode Status { get; set; }
    }
}