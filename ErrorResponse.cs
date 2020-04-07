using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Api.Startup
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn , Id = "Error")]
    public class ErrorResponse
    {
        [JsonProperty]
        public string Message { get; set; }
    }
}
