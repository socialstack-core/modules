using System;
using System.Collections;
using System.Collections.Generic;
using Api.AutoForms;
using Api.Configuration;
using Api.Database;
using Api.Permissions;


namespace Api.Eventing
{
	/// <summary>
	/// A grouping of common events, such as before/ after create, update, delete etc.
	/// These are typically added to the Events class, named directly after the type that is being used.
	/// </summary>
	public partial class EventGroup<T>
	{
		#region Service events

		/// <summary>
		/// Called when this service is being configured.
		/// This is either when its config is first loaded, or when it was updated.
		/// </summary>
		public EventHandler<Config> Configure;
		
		#endregion
	}
}