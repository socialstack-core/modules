using System;
using System.Threading;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Api.Startup;

namespace Api.Presence
{
    /// <summary>
    /// Background task that cleans up the page presence records for this server 
    /// </summary>
    public class PagePrescenceHostedService : AutoService
    {
        /// <summary>
        /// Timer for the updates, fires every n seconds
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// Entry point for the hosted service
        /// </summary>
        /// <returns></returns>
        public PagePrescenceHostedService()
        {
            Api.Eventing.Events.Service.AfterStart.AddEventListener((Context ctx, object svc) =>
            {
                _timer = new Timer(CleanUpStaleRecords, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
                return new ValueTask<object>(svc);
            });
        }

        /// <summary>
        /// This really is the last stand for any user presence that has got out of sync
        /// </summary>
        /// <param name="state"></param>
        private async void CleanUpStaleRecords(object state)
        {
            PagePresenceRecordService service = Services.Get<PagePresenceRecordService>();

            //Get all the records in this machine that have not been refreshed in 30 seconds
            if (service != null)
            {
                var context = new Context();
                
                var priorServerEntries = await service
                    .Where("ServerId=? and EditedUtc<?", DataOptions.IgnorePermissions)
                    .Bind(service.ServerId)
                    .Bind(DateTime.UtcNow.AddMinutes(5))
                    .ListAll(context);

                foreach (var entry in priorServerEntries)
                {
                    await service.Delete(context, entry, DataOptions.IgnorePermissions);
                }
            }
        }
    }
}