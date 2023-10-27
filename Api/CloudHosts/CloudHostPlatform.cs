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
        /// The URL for the upload host (excluding /content/) if this host platform is providing file services.
        /// </summary>
        /// <returns></returns>
        public virtual string GetContentUrl()
        {
            return null;
        }

        /// <summary>
        /// Lists files from the storage area into the given file meta stream.
        /// The source can cancel the request via cancelling the meta stream.
        /// </summary>
        /// <param name="metaStream"></param>
        /// <returns></returns>
        public virtual Task ListFiles(FileMetaStream metaStream)
        {
			throw new NotImplementedException();
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
        /// Reads a files bytes from the remote host.
        /// </summary>
        /// <param name="locator">Describes info about a file including its relative path and if it is private or not.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual Task<System.IO.Stream> ReadFile(FilePartLocator locator)
        {
            throw new NotImplementedException();
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