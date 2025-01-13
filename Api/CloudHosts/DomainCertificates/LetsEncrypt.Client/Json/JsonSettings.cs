using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LetsEncrypt.Client.Json
{
    /// <summary>
    /// </summary>
    public static class JsonSettings
    {
        /// <summary>
        /// </summary>
        public static JsonSerializerSettings CreateSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
        }
    }
}