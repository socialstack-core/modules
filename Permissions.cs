using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Revisions
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
				// Block all revision EPs:
				Roles.Member.RevokeIfEndsWith("revision_list", "revision_create", "revision_load", "revision_delete");
				Roles.Guest.RevokeIfEndsWith("revision_list", "revision_create", "revision_load", "revision_delete");
				Roles.Public.RevokeIfEndsWith("revision_list", "revision_create", "revision_load", "revision_delete");

				return new ValueTask<object>(source);
			});
		}
	}
}