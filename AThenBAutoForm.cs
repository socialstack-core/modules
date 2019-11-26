using Api.AutoForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.IfAThenB
{
	/// <summary>
	/// Used when creating or updating an a then b rule.
	/// </summary>
	public partial class AThenBAutoForm : AutoForm<AThenB>
	{
		/// <summary>
		/// The name of the event.
		/// </summary>
		public string EventName;
		
		/// <summary>
		/// The name of the action to trigger.
		/// </summary>
		public string ActionName;
		
		/// <summary>
		/// Config for the action.
		/// </summary>
		public string ActionConfigJson;
	}

}