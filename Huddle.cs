using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A Huddle
	/// </summary>
	public partial class Huddle : RevisionRow
	{

		/// <summary>
		/// The server DNS address where this huddle is hosted.
		/// huddler14.site.com for example.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string ServerAddress;

	}

}