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
				Roles.Member.RevokeFeature("RevisionList", "RevisionCreate", "RevisionLoad", "RevisionDelete");
				Roles.Guest.RevokeFeature("RevisionList", "RevisionCreate", "RevisionLoad", "RevisionDelete");
				Roles.Public.RevokeFeature("RevisionList", "RevisionCreate", "RevisionLoad", "RevisionDelete");

				return new ValueTask<object>(source);
			});
		}
	}
}