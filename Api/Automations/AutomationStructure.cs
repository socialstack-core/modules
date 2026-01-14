using Api.Database;
using Api.Startup;
using System;
using System.Collections.Generic;

namespace Api.Automations
{
	/// <summary>
	/// Defines what automations are available from this API
	/// </summary>
	public class AutomationStructure
	{
		/// <summary>
		/// The automations in this API.
		/// </summary>
		public List<Automation> Results { get; set; }
	}
	
	/// <summary>
	/// Information about a particular automation.
	/// </summary>
	public class Automation
	{
		/// <summary>
		/// The name of the automation.
		/// </summary>
		public string Name;

        /// <summary>
        /// The description of the automation.
        /// </summary>
        public string Description;

        /// <summary>
        /// The description of the automations cron.
        /// </summary>
        public string CronDescription;
		
		/// <summary>
		/// The cron schedule for the automation.
		/// </summary>
		public string Cron;

		private AutomationRunInfo _runInfo;

		/// <summary>
		/// Last trigger time
		/// </summary>
		public DateTime? LastTrigger => _runInfo.LastTrigger;


		/// <summary>
		/// Creates an automation description object for the given run info.
		/// </summary>
		/// <param name="runInfo"></param>
		public Automation(AutomationRunInfo runInfo)
		{
			_runInfo = runInfo;
		}
	}
}
