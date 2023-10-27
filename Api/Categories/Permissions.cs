using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Categories
{
	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class Permissions
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Permissions()
		{
			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
			{
				// Public - the role used by anonymous users.
				Roles.Guest.Revoke("category_delete", "category_update");
				Roles.Member.Revoke("category_delete", "category_update");
				return new ValueTask<object>(source);
			});
		}
	}
}