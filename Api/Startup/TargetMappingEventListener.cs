using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;
using System.Linq;

namespace Api.Startup
{

    /// <summary>
    /// Listens out for target service shutdown and removes mappings
    /// For example from customtypes when fields are added 
    /// </summary>
    [EventListener]
    public class TargetMappingEventListener
    {
        /// <summary>
        /// When a service is removed, ensure that any mappings to it are removed 
        /// They will then be regenerated
        /// </summary>
        public TargetMappingEventListener()
        {
            Events.Service.BeforeDelete.AddEventListener((Context ctx, AutoService targetsvc) =>
            {
                foreach (var kvp in Services.All)
                {
                    var srcService = kvp.Value;
                    if (srcService == null || srcService.GeneratedMappings == null)
                    {
                        continue;
                    }

                    var mapping = srcService.GeneratedMappings.FirstOrDefault(gm => gm.Target == targetsvc);
                    if (mapping == null)
                    {
                        continue;
                    }

                    MappingTypeEngine.Remove(srcService, targetsvc);
                }
                return new ValueTask<AutoService>(targetsvc);
            }, 2);
        }
    }
}