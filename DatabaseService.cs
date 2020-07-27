using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.DatabaseDiff;


namespace Api.Database
{
	/// <summary>
	/// Handles communication with the sites database.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IDatabaseService
    {
		
		/// <summary>
		/// The latest DB schema.
		/// </summary>
		Schema Schema {get; set;}
		
	}
	
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