using Api.Automations;
using Api.Documentations;
using Api.Permissions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// The cron scheduler for Automations.
		/// </summary>
		private static CronScheduler _cronScheduler;

		/// <summary>
		/// Takes a cron string and returns an event handler which you can add a listener to. Generally used during startup.
		/// The event will trigger at the rate specified by your cron expression.
		/// Provide a name (typically lowercase with underscores instead of spaces) such that you can also explicitly request the automation to run from its name too.
		/// Note that if you provide a name but not the cron expression, you can add additional event handlers to an existing job.
		/// </summary>
		public static EventHandler<AutomationRunInfo> Automation(string name, string cronExpression = null)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}

			// Lowercased name:
			name = name.ToLower();

			if (_cronScheduler == null)
			{
				_cronScheduler = new CronScheduler();
			}
			
			if (!_cronScheduler.TryGetAutomation(name, out AutomationRunInfo runInfo))
			{
				runInfo = new AutomationRunInfo()
				{
					Name = name,
					Events = new EventHandler<AutomationRunInfo>()
				};
				_cronScheduler.AddToLookup(runInfo);
			}

			if (cronExpression != null)
			{
				if (runInfo.Cron != null)
				{
					if (cronExpression != runInfo.Cron)
					{
						throw new Exception(
							"Automation name collision: You tried to create an automation called '" + name + "' but it already exists with a different schedule. " +
							"If you just wanted to add an event handler to the existing job, don't specify the cron schedule - i.e. use Automation(\"" + name + "\") instead."
						);
					}

					// Stop there - schedule already defined.
					return runInfo.Events;
				}

				// This call is defining when the job will actually run.
				runInfo.Cron = cronExpression;
				runInfo.CronExpression = new CronExpression(runInfo.Cron);

				_cronScheduler.Schedule(runInfo);
			}

			return runInfo.Events;
		}

	}
}