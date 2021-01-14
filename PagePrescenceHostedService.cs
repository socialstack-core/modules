using System;
using System.Threading;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Api.Presence
{
    /// <summary>
    /// Event listener to start up the cleanup job for sending tracking information
    /// </summary>
    [EventListener]
    public class EventsListner
    {
        public EventsListner()
        {
            WebServerStartupInfo.OnConfigureServices += obj =>
            {
                obj.AddHostedService<PagePrescenceHostedService>();
            };

        }
    }
    
    /// <summary>
    /// Background task that cleans up the page presence records for this server 
    /// </summary>
    public class PagePrescenceHostedService : IHostedService, IDisposable
    {
        /// <summary>
        /// Timer for the updates, fires every n seconds
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// Entry point for the hosted service
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(CleanUpStaleRecords, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
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
                var priorServerEntries = await service.List(context, new Filter<PagePresenceRecord>().Equals("ServerId", service.ServerId).And().LessThan("EditedUtc" , DateTime.UtcNow.AddMinutes(5)));

                foreach (var entry in priorServerEntries)
                {
                    await service.Delete(context, entry);
                }
            }
        }

        /// <summary>
        /// Called when the job needs to stop
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }


        /// <summary>
        /// disposal
        /// </summary>
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}