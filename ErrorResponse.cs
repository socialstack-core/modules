using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Api.Startup
{
    /// <summary>
    /// Used when responding with an error
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn , Id = "Error")]
    public class ErrorResponse
    {
        /// <summary>
        /// The error message
        /// </summary>
        [JsonProperty]
        public string Message { get; set; }
    }
}
