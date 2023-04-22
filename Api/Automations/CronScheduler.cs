using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Api.ColourConsole;

namespace Api.Automations;


/// <summary>
/// Cron scheduler.
/// </summary>
public class CronScheduler
{
	/// <summary>
	/// The scheduler is paused if it has nothing to wait for.
	/// </summary>
	public bool Paused => _firstToRun == null;

	/// <summary>
	/// Maps automation names to the run info. The name is lowercased.
	/// </summary>
	private Dictionary<string, AutomationRunInfo> _automationsByName = new Dictionary<string, AutomationRunInfo>();

	private AutomationRunInfo _firstToRun;
	private Timer _timer;
	private object _scheduleQ = new object();
	private DateTime _lastUpdated;
	/// <summary>
	/// The last time something was added to the schedule.
	/// </summary>
	public DateTime LastUpdated => _lastUpdated;

	/// <summary>
	/// A readonly set of the automations by name.
	/// </summary>
	public Dictionary<string, AutomationRunInfo> AutomationsByName => _automationsByName;

	/// <summary>
	/// Triggers an automation by its name.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public async ValueTask Trigger(string name)
	{
		name = name.ToLower();

		if (TryGetAutomation(name, out AutomationRunInfo runInfo))
		{
			await runInfo.Trigger();
		}
	}

	/// <summary>
	/// Adds the given info to the automations lookup.
	/// </summary>
	/// <param name="info"></param>
	public void AddToLookup(AutomationRunInfo info)
	{
		_automationsByName[info.Name] = info;
		_lastUpdated = DateTime.UtcNow;
	}

	/// <summary>
	/// Tries to get an automation by lowercase name.
	/// </summary>
	/// <param name="lcName"></param>
	/// <param name="info"></param>
	/// <returns></returns>
	public bool TryGetAutomation(string lcName, out AutomationRunInfo info)
	{
		return _automationsByName.TryGetValue(lcName, out info);
	}

	/// <summary>
	/// Schedules the given automation.
	/// </summary>
	public void Schedule(AutomationRunInfo runInfo)
	{
		if (runInfo.Scheduled)
		{
			return;
		}

		// Update its next ticks:
		var nrt = runInfo.UpdateNextTicks();

		if (!nrt.HasValue)
		{
			WriteColourLine.Warning("[WARN] Asked to schedule an automation '" + runInfo.Name + "' but ignoring it because it will never run.");
			return;
		}

		_lastUpdated = DateTime.UtcNow;

		lock (_scheduleQ)
		{
			AddToScheduler(runInfo, nrt.Value);
		}

		// Globally shared timer.
		if (_timer == null)
		{
			_timer = new Timer();
			_timer.Elapsed += (object source, ElapsedEventArgs e) => {

				var now = DateTime.UtcNow.Ticks;

				// Likely running at least the first one.
				lock (_scheduleQ)
				{
					var current = _firstToRun;

					while (current != null)
					{
						var next = current.After;
						var nrt = current.NextRunTicks;

						if (!nrt.HasValue)
						{
							// Remove it. This shouldn't be here.
							_firstToRun = next;
							current.Scheduled = false;
							current.After = null;
						}
						else
						{
							var ticks = nrt.Value;

							if (ticks < now)
							{
								// Remove it from the schedule queue:
								_firstToRun = next;
								current.After = null;
								current.Scheduled = false;

								// Trigger the automation, which will re-add it at its next run time:
								TriggerScheduledAutomation(current);
							}
						}

						current = next;
					}
				}
				
			};

			_timer.Interval = 1000;
			_timer.Enabled = true;
		}

	}

	private void AddToScheduler(AutomationRunInfo runInfo, long newTicks)
	{
		if (runInfo.Scheduled)
		{
			return;
		}

		runInfo.Scheduled = true;

		if (_firstToRun == null)
		{
			// First one here.
			_firstToRun = runInfo;
			runInfo.After = null;
		}
		else
		{
			// insert in to the linked list based on the ticks value. It goes in front of the first one encountered that has ticks higher than it.
			var current = _firstToRun;
			AutomationRunInfo prev = null;

			while (current != null)
			{
				var ticks = current.NextRunTicks;

				if (ticks.HasValue && ticks.Value > newTicks)
				{
					// This one is after the new automation.
					// Insert the new automation before it.
					if (prev == null)
					{
						runInfo.After = _firstToRun;
						_firstToRun = runInfo;
					}
					else
					{
						runInfo.After = prev.After;
						prev.After = runInfo;
					}

					return;
				}

				prev = current;
				current = current.After;
			}

			// It wasn't before any of them, meaning it is after the last one.
			prev.After = runInfo;
			runInfo.After = null;
		}
	}

	private void TriggerScheduledAutomation(AutomationRunInfo toRun)
	{
		_ = Task.Run(async () => {
			try
			{
				await toRun.Trigger();
			}
			catch (Exception ex)
			{
                WriteColourLine.Error("[ERROR] An automation threw an error: " + ex.ToString());
			}

			// Update its next run time.
			var nextRun = toRun.UpdateNextTicks();

			if (nextRun.HasValue)
			{
				// Based on next run value, re-add scheduler queue.
				AddToScheduler(toRun, nextRun.Value);
			}
			else
			{
				toRun.Scheduled = false;
                WriteColourLine.Info("[INFO] Automation '" + toRun.Name + "' just ran for the last time.");
			}
		});
	}

}