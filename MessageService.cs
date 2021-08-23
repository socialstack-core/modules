using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Messages
{
	/// <summary>
	/// Handles messages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class MessageService : AutoService<Message>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MessageService() : base(Events.Message)
        {
			// Example admin page install:
			// InstallAdminPages("Messages", "fa:fa-rocket", new string[] { "id", "name" });

			Events.Message.BeforeCreate.AddEventListener(async (Context context, Message message) =>
			{
				if(context == null || message == null)
                {
					return message;
                }

				message.UserId = context.UserId;

				return message;
			});
		}
	}
    
}
