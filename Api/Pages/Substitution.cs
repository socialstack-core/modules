using Api.Contexts;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.Pages
{
	/// <summary>
	/// A substitution for a {KEY} within generated HTML.
	/// </summary>
	public partial class Substitution
	{
		
		/// <summary>
		/// The key in the HTML.
		/// </summary>
		public string Key;
		
		/// <summary>
		/// The method to run when this sub occurs.
		/// </summary>
		public Action<Context> OnGenerate;
		
	}
	
}