using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Persons
{
	/// <summary>
	/// Handles people who aren't users - used by e.g. staff lists.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPersonService
    {
		/// <summary>
		/// Delete a person by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a person by its ID.
		/// </summary>
		Task<Person> Get(Context context, int id);

		/// <summary>
		/// Create a new person.
		/// </summary>
		Task<Person> Create(Context context, Person prj);

		/// <summary>
		/// Updates the database with the given person data. It must have an ID set.
		/// </summary>
		Task<Person> Update(Context context, Person prj);

		/// <summary>
		/// List a filtered set of people.
		/// </summary>
		/// <returns></returns>
		Task<List<Person>> List(Context context, Filter<Person> filter);

	}
}
