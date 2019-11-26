using Microsoft.Extensions.Configuration;
using Api.Configuration;
using System.Threading.Tasks;

namespace Api.Database
{
	/// <summary>
	/// Handles communication with the sites database.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class DatabaseService : DatabaseServiceCore, IDatabaseService
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public DatabaseService() : base (AppSettings.Configuration.GetConnectionString("DefaultConnection")){
			
		}
	}
}
