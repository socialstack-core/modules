using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Configuration;
using Microsoft.Extensions.Configuration;
using Api.Uploader;
using Api.Startup;

namespace Api.FileTypeBlocker
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class FileTypeBlockerService : AutoService
    {
		private FileTypeBlockerConfig _configuration;

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
			_configuration = GetConfig<FileTypeBlockerConfig>();

			_configuration.OnChange += () => {
				UpdateList();
				return new ValueTask();
			};

			UpdateList();
		}

		private void UpdateList()
		{
			FilterSet = new Dictionary<string, bool>();
			UseWhitelist = false;

			if (_configuration == null)
			{
				// Do nothing.
				return;
			}

			UseWhitelist = _configuration.UseWhitelist;

			var filterSet = UseWhitelist ? _configuration.Whitelist : _configuration.Blacklist;

			if (filterSet != null)
			{

				foreach (var ext in filterSet)
				{
					// Add:
					FilterSet[ext.Trim().ToLower()] = true;
				}
			}

			Events.Upload.BeforeCreate.AddEventListener((Context context, Upload upload) => {
				
				if(upload == null || (context.Role != null && context.Role.CanViewAdmin))
				{
					// Admins also aren't restricted.
					return new ValueTask<Upload>(upload);
				}
				
				// If whitelist mode, FileType must be in the set.
				// If blacklist mode, FileType must not be in the set.
				// Thus:
				if(FilterSet.ContainsKey(upload.FileType) != UseWhitelist)
				{
					// Reject:
					throw new PublicException("'" + upload.FileType + "' files can't be uploaded here.", "file_type");
				}
				
				return new ValueTask<Upload>(upload);
			});
		}
	}
    
}
