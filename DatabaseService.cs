using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.DatabaseDiff;


namespace Api.Database
{
	/// <summary>
	/// Instanced automatically.
	/// </summary>
	public partial class DatabaseService{
		
		/// <summary>
		/// The latest DB schema.
		/// </summary>
		public Schema Schema {get; set;}
		
	}
	
}