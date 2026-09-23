using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.MediaShares
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
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
			{
				Roles.Guest.Revoke("mediaShare_load", "mediaShare_list");
				Roles.Public.Revoke("mediaShare_load", "mediaShare_list");
				Roles.Member.Revoke("mediaShare_load", "mediaShare_list");
				Roles.Member.If("IsSelf()").ThenGrant("mediaShare_load", "mediaShare_list");
				Roles.Member.Grant("mediaShare_create");

				return new ValueTask<object>(source);
			}, 20);
		}
	}
}