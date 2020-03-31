using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;
using Api.AutoForms;

namespace Api.IfAThenB
{

	/// <summary>
	/// An if _ then _ rule.
	/// </summary>
	public partial class AThenB : RevisionRow
	{
		/// <summary>
		/// The name of the event.
		/// </summary>
		[Module("Admin/Event/Select")]
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