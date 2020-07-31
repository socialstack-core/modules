using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Microsoft.Extensions.Configuration;
using Api.Configuration;
using Microsoft.Extensions.Hosting;
using Api.Users;
using System;

namespace Api.DefaultAvatar
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class DefaultAvatarService : IDefaultAvatarService
    {
		private DefaultAvatarConfig _configuration; 
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public DefaultAvatarService()
        {
			var section = AppSettings.GetSection("DefaultAvatars");

			if (section == null)
			{
				return;
			}

			_configuration = section.Get<DefaultAvatarConfig>();

			if (_configuration == null)
			{
				return;
			}

			var images = _configuration.Images;
			
			if(images == null || images.Length == 0)
			{
				return;
			}

			var rand = new Random();

			Events.User.BeforeCreate.AddEventListener((Context ctx, User user) => {

				if (string.IsNullOrEmpty(user.AvatarRef))
				{
					user.AvatarRef = images[rand.Next(0, images.Length)];
				}

				return Task.FromResult(user);
			}, 6);
			
		}
	}
    
}
