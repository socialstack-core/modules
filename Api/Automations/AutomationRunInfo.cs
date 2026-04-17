
using Api.AutomationTasks;
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
		/// True if this cron is running.
		/// </summary>
		public bool IsRunning;

		/// <summary>
		/// True if this crons most recent run failed with an exception.
		/// </summary>
		public bool LastRunFailed;

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
        /// The description of this run info.
        /// </summary>
        public string Description;

		/// <summary>
		/// The set of event handlers on this run info.
		/// </summary>
		public Api.Eventing.EventHandler<AutomationRunInfo> Events;

		/// <summary>
		/// The next time this automation will run at (in ticks).
		/// </summary>
		internal long? NextRunTicks;

		/// <summary>
		/// The ID of the AutomationTask record. Set once by ObtainLease and never cleared.
		/// </summary>
		public uint TaskId;

		/// <summary>
		/// Current active lease, or null if this automation is not currently running.
		/// </summary>
		internal AutomationTask ActiveLease;

		/// <summary>
		/// Updates the next time this runs.
		/// </summary>
		public long? UpdateNextTicks()
		{
			if (Scheduled) {
				// Must not recalculate if already scheduled.
				return NextRunTicks;
			}

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

		private DateTime? lastTrigger;

		/// <summary>
		/// Last trigger time.
		/// </summary>
		public DateTime? LastTrigger => lastTrigger;

		/// <summary>
		/// Triggers the automation.
		/// </summary>
		/// <returns></returns>
		public async ValueTask Trigger()
		{
			lastTrigger = DateTime.UtcNow;
			await Events.Dispatch(new Context(1, 0, 1), this);
		}

	}
}