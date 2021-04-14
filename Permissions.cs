using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.PhotosphereTracking
{
    /// <summary>
    /// Instances capabilities during the very earliest phases of startup.
    /// </summary>
    [EventListener]
    public class Permissions
    {
        public Permissions()
        {
            Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
            {
                Roles.Guest.Grant("photospheretrack_create");
                Roles.Member.Grant("photospheretrack_create");

                return new ValueTask<object>(source);
            }, 20);
        }
    }
}
