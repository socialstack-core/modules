using System;


namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		#region Service events

		/// <summary>
		/// This is invoked just after services are instanced.
		/// Use it to add additional types, or block adding this one.
		/// </summary>
		public static EventHandler<object> ServicesAfterStart;
		
		#endregion
		
	}

}
