using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


namespace Api.DatabaseDiff
{

	/// <summary>
	/// Listens for the configure event so it can sync the DB schemas 
	/// before other services are instanced.
	/// </summary>
	[EventListener]
	public class EventListener
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Hook up the configure app method:
			Events.ServicesAfterStart.AddEventListener((Context ctx, object sender) => { 

				// Get the DB diff service:
				var dbDiff = Services.Get<IDatabaseDiffService>();

				// Diff now:
				dbDiff.UpdateDatabaseSchema();

				return Task.FromResult(sender);
			});
		}

	}
}
