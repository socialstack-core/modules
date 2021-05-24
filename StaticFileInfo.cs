using System;


namespace Api.CanvasRenderer
{
	
	/// <summary>
	/// Info about a static file.
	/// </summary>
	public class StaticFileInfo
	{
		
		/// <summary>
		/// File size.
		/// </summary>
		public long Size;
		
		/// <summary>
		/// Last modified date, in terms of Ticks/10000.
		/// </summary>
		public ulong ModifiedUtc;
		
		/// <summary>
		/// The ref for the file. Of the form s:/...
		/// </summary>
		public string Ref;
		
	}
	
}