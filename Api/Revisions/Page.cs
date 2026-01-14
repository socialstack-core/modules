using System;
using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Pages
{
	
	/// <summary>
	/// A page. Pages are accessed via associated permalink(s).
	/// </summary>
	public partial class Page
	{
		/// <summary>
		/// True if the primary content should load from a revision with the specified ID rather than the base class.
		/// </summary>
		public bool PrimaryContentIsRevisions;
	}
	
}