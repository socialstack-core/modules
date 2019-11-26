using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;

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