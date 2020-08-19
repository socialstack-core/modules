using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Configuration;
using Microsoft.Extensions.Configuration;
using Api.Uploader;

namespace Api.FileTypeBlocker
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class FileTypeBlockerService : IFileTypeBlockerService
    {
		private FileTypeBlockerConfig _configuration;

		/// <summary>
		/// Default whitelist
		/// </summary>
		public readonly string[] WhitelistDefaults = new string[]{
			"png",
			"jpg",
			"jpeg",
			"tiff",
			"tif",
			"bmp",
			"heif",
			"heic",
			"gif",
			"webp",
			"webm",
			"mp3",
			"pdf",
			"svg",
			"svgz",
			"jp2",
			"j2k",
			"jpf",
			"jpx",
			"jpm",
			"mj2",
			"wav",
			"mp4",
			"webm",
			"mpeg",
			"mpg",
			"mp2",
			"avi",
			"mov"
		};
		
		/// <summary>
		/// Default blacklist
		/// </summary>
		public readonly string[] BlacklistDefaults = new string[]{
			"exe",
			"msi",
			"pif",
			"application",
			"app",
			"com",
			"scr",
			"hta",
			"cpl",
			"msc",
			"jar",
			"js",
			"jse",
			"html",
			"css",
			"bat",
			"cmd",
			"sh",
			"php",
			"ps1",
			"ps1xml",
			"ps2",
			"scf",
			"lnk",
			"inf",
			"reg",
			"ps2xml",
			"psc1",
			"psc2",
			"htaccess",
			"aspx",
			"asp",
			"zip",
			"rar",
			"tar",
			"gz",
			"img",
			"iso",
			"bin"
		};
		
		/// <summary>
		/// The constructed filter set.
		/// </summary>
		private Dictionary<string, bool> FilterSet;
		
		/// <summary>
		/// True if it's using a whitelist technique.
		/// </summary>
		private bool UseWhitelist;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public FileTypeBlockerService()
        {
			_configuration = AppSettings.GetSection("FileTypeBlocker").Get<FileTypeBlockerConfig>();
			
			var useWhitelist = false;
			var blacklist = BlacklistDefaults;
			var whitelist = WhitelistDefaults;
			
			if(_configuration != null)
			{
				if(_configuration.Blacklist != null)
				{
					blacklist = _configuration.Blacklist;
				}
				
				if(_configuration.Whitelist != null)
				{
					whitelist = _configuration.Whitelist;
				}
				
				useWhitelist = _configuration.UseWhitelist;
			}
			
			var filterSet = useWhitelist ? whitelist : blacklist;
			FilterSet = new Dictionary<string, bool>();
			
			foreach(var ext in filterSet)
			{
				// Add:
				FilterSet[ext.Trim().ToLower()] = true;
			}
			
			UseWhitelist = useWhitelist;
			
			Events.Upload.BeforeCreate.AddEventListener((Context context, Upload upload) => {
				
				if(upload == null)
				{
					return Task.FromResult((Upload)null);
				}
				
				// If whitelist mode, FileType must be in the set.
				// If blacklist mode, FileType must not be in the set.
				// Thus:
				if(FilterSet.ContainsKey(upload.FileType) != UseWhitelist)
				{
					// Reject:
					return Task.FromResult((Upload)null);
				}
				
				return Task.FromResult(upload);
			});
		}
	}
    
}
