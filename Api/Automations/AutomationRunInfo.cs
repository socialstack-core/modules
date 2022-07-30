
using Api.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Automations
{
	/// <summary>
	/// Information about this trigger of an automation.
	/// </summary>
	public class AutomationRunInfo
	{

		/// <summary>
		/// The cron expression this run info is scheduled on.
		/// </summary>
		public string Cron;

		/// <summary>
		/// True if this runInfo has been added to the scheduler.
		/// </summary>
		internal bool Scheduled;

		/// <summary>
		/// The parsed cron expression.
		/// </summary>
		public CronExpression CronExpression;

		/// <summary>
		/// The automation to run after this one.
		/// </summary>
		internal AutomationRunInfo After;

		/// <summary>
		/// The lowercased name of this run info.
		/// </summary>
		public string Name;

		/// <summary>
		/// The context the automation uses.
		/// </summary>
		public Context Context = new Context(1,1,1);

		/// <summary>
		/// The set of event handlers on this run info.
		/// </summary>
		public Api.Eventing.EventHandler<AutomationRunInfo> Events;

		/// <summary>
		/// The next time this automation will run at (in ticks).
		/// </summary>
		internal long? NextRunTicks;

		/// <summary>
		/// Updates the next time this runs.
		/// </summary>
		public long? UpdateNextTicks()
		{
			var dt = DateTime.UtcNow;
			var next = CronExpression.GetNextValidTimeAfter(dt);

			long? nrt;

			if (next.HasValue)
			{
				nrt = next.Value.DateTime.Ticks;
			}
			else
			{
				nrt = null;
			}

			NextRunTicks = nrt;
			return nrt;
		}

		/// <summary>
		/// Triggers the automation.
		/// </summary>
		/// <returns></returns>
		public async ValueTask Trigger()
		{
			await Events.Dispatch(Context, this);
		}

	}
}