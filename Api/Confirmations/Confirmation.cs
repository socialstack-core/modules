using System;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Confirmations
{
	
	/// <summary>
	/// A Confirmation. Typically indicates that a user has accepted (or rejected) a particular request.
	/// </summary>
	[ListAs("Confirmations", Explicit = true)]
	public partial class Confirmation : UserCreatedContent<uint>
	{
		/// <summary>
		/// True if this is an acceptation.
		/// </summary>
		public bool IsAccept;
	}

}