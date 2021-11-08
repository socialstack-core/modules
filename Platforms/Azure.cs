using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using Api.Uploader;
using Api.Contexts;
using System;

namespace Api.CloudHosts
{

    /// <summary>
    /// Azure config.
    /// </summary>
    public partial class AzureConfig
    {
    }
    
    /// <summary>
    /// A representation of Azure.
    /// </summary>
    public partial class AzureHost : CloudHostPlatform
    {
        // private CloudHostService _cloudHost;
        private AzureConfig _config;


        /// <summary>
        /// Creates a new Azure host, shared by all requests.
        /// </summary>
        /// <param name="chs"></param>
        /// <param name="config"></param>
        public AzureHost(CloudHostService chs, AzureConfig config)
        {
            //_cloudHost = chs;
            _config = config;
			
			// Azure blob storage todo in here
			// SetConfigured("upload");
        }
		
		/*
        /// <summary>
        /// Runs when uploading a file.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="upload"></param>
        /// <param name="tempFile"></param>
        /// <param name="variantName"></param>
        /// <returns></returns>
        public override async Task<bool> Upload(Context context, Upload upload, string tempFile, string variantName)
        {
            return false;
        }
		*/
		
    }
 
}