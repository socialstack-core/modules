using Api.Contexts;
using Api.Uploader;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.CloudHosts
{
    /// <summary>
    /// Host platform.
    /// </summary>
    public partial class CloudHostPlatform
    {
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, bool> configuredState = new Dictionary<string, bool>();

        /// <summary>
        /// Sets the given service as configured.
        /// </summary>
        /// <param name="key"></param>
        protected void SetConfigured(string key)
        {
            configuredState[key] = true;
        }

        /// <summary>
        /// True if this host platform has the given service type configured. Key is e.g. "upload".
        /// </summary>
        public virtual bool HasService(string serviceType)
        {
            configuredState.TryGetValue(serviceType, out bool val);
            return val;
        }

        /// <summary>
        /// Runs when uploading a file.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="upload"></param>
        /// <param name="tempFile"></param>
        /// <param name="variantName"></param>
        /// <returns></returns>
        public virtual Task<bool> Upload(Context context, Upload upload, string tempFile, string variantName)
        {
            throw new NotImplementedException();
        }
    }
}