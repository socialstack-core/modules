using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;


namespace Api.Eventing
{
	/// <summary>
	/// The placement of an event handler.
	/// This gets derived from the name - see EventHandler for more details.
	/// </summary>
	public enum EventPlacement : int {
		/// <summary>
		/// Any event placement.
		/// </summary>
		Any = 1 | 2 | 4,
		/// <summary>
		/// Event occurs before something.
		/// </summary>
		Before = 4,
		/// <summary>
		/// Event occurs after something.
		/// </summary>
		After = 2,
		/// <summary>
		/// Event occurs during something.
		/// </summary>
		On = 1,
		/// <summary>
		/// It's not been specified.
		/// </summary>
		NotSpecified = 0
	}
}
