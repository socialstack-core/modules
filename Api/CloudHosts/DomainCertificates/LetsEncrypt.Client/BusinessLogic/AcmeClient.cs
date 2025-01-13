using LetsEncrypt.Client.Json;
using Newtonsoft.Json;
using System;

namespace LetsEncrypt.Client
{
    /// <summary>
    /// </summary>
    public partial class AcmeClient : BaseAcmeClient
    {
        private readonly JsonSerializerSettings _jsonSettings = JsonSettings.CreateSettings();

        // Ctor

        /// <summary>
        /// </summary>
        public AcmeClient(Uri directoryUri)
            : base(directoryUri)
        {
        }
    }
}