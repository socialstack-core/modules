using Microsoft.Extensions.Configuration;
using Api.Configuration;
using System.Threading.Tasks;
using Api.Startup;

namespace Api.Database
{
	/// <summary>
	/// Handles communication with the sites database.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(1)]
	public partial class DatabaseService : DatabaseServiceCore
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public DatabaseService() : base (AppSettings.Configuration.GetConnectionString(System.Environment.GetEnvironmentVariable("ConnectionStringName") ?? "DefaultConnection")){
			
		}
	}
}
