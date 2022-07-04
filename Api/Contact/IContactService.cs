using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Contacts
{
	/// <summary>
	/// Handles contact messages - these are messages sent from the UI to contact the developers. In the future, it would be nice if this also sent an email.
	/// </summary>
	public partial interface IContactService
    {
		/// <summary>
		/// Delete a contact by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a contact by its ID.
		/// </summary>
		Task<Contact> Get(Context context, int id);

		/// <summary>
		/// Create a new contact.
		/// </summary>
		Task<Contact> Create(Context context, Contact contact);

		/// <summary>
		/// Updates the database with the given contact data. It must have an ID set.
		/// </summary>
		Task<Contact> Update(Context context, Contact contact);

		/// <summary>
		/// List a filtered set of contacts.
		/// </summary>
		/// <returns></returns>
		Task<List<Contact>> List(Context context, Filter<Contact> filter);

	}
}
