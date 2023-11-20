using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Pages;
using Api.Startup;
using System;
using System.Threading.Tasks;

namespace Api.ThirdParty.Pages
{
    [LoadPriority(101)]
    public class CacheHandlerService : AutoService
    {
        private readonly HtmlService _htmlService;

        public CacheHandlerService(HtmlService htmlService)
        {
            _htmlService = htmlService;

#warning disabled
/*
			var setupForTypeMethod = GetType().GetMethod(nameof(SetupForType));

			Events.Service.AfterCreate.AddEventListener((Context context, AutoService service) => {

				if (service == null)
				{
					return new ValueTask<AutoService>(service);
				}

				// Get the content type for this service and event group:
				var servicedType = service.ServicedType;

				if (servicedType == null)
				{
					return new ValueTask<AutoService>(service);
				}

				// Add List event:
				var setupType = setupForTypeMethod.MakeGenericMethod(new Type[] {
					servicedType,
					service.IdType
				});

				setupType.Invoke(this, new object[] {
					service
				});

				return new ValueTask<AutoService>(service);
			}, 1);
*/

		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="service"></param>
        public void SetupForType<T, ID>(AutoService<T, ID> service)
            where T : Content<ID>, new()
            where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
        {
			if (service.EntityName.EndsWith("_PersonalRoomServers") || service.EntityName.Contains("ClusteredServer")
				|| service.EntityName.Contains("ItineraryContactForm") || service.EntityName.Contains("StandardContactForm") // Temp hack to stop cache purge on form submission
				|| typeof(T) == typeof(Uploader.Upload))
			{
				// Websocket rooms. Created when logged in people arrive and depart on the site.
				return;
			}

			var editLine = "Cache cleared by edit of " + service.EntityName;
			var createLine = "Cache cleared by creation of " + service.EntityName;

			service.EventGroup.AfterUpdate.AddEventListener(async (Context ctx, T entity) =>
            {
				Log.Info("htmlcache", editLine);
                _htmlService.ClearCache();

                return entity;
            });

            service.EventGroup.AfterCreate.AddEventListener(async (Context ctx, T entity) =>
			{
				Log.Info("htmlcache", createLine);
				_htmlService.ClearCache();

                return entity;
            });
        }
    }
}
