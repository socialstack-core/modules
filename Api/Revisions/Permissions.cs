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
				Roles.Member.RevokeFeature((Capability cap) => {
					var pattern = "_revisions_" + cap.Feature;
					return cap.Name.EndsWith(pattern);
				}, "list", "create", "load", "delete");

				Roles.Guest.RevokeFeature((Capability cap) => {
					var pattern = "_revisions_" + cap.Feature;
					return cap.Name.EndsWith(pattern);
				}, "list", "create", "load", "delete");

				Roles.Public.RevokeFeature((Capability cap) => {
					var pattern = "_revisions_" + cap.Feature;
					return cap.Name.EndsWith(pattern);
				}, "list", "create", "load", "delete");

				return new ValueTask<object>(source);
			});
		}
	}
}