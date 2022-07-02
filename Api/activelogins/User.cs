using Api.Database;
using Api.Tags;
using System.Collections.Generic;

namespace Api.Users{
	
	public partial class User
	{
		
		/// <summary>
		/// 0 = Offline, 1 = Online, 2 = Away (away unused at the moment).
		/// </summary>
		public int? OnlineState;
		
	}
	
}