using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.ProfanityFilter
{
	/// <summary>
	/// Handles profanityFilter.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IProfanityFilterService
    {

		/// <summary>
		/// Returns the number of profanity filter hits the given text has.
		/// </summary>
		int Measure(string text);

	}
}
