using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Emails
{
	/// <summary>
	/// Handles emailTemplates.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IEmailTemplateService
    {
		/// <summary>
		/// Delete an emailTemplate by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get an emailTemplate by its ID.
		/// </summary>
		Task<EmailTemplate> Get(Context context, int id);

		/// <summary>
		/// Create an emailTemplate.
		/// </summary>
		Task<EmailTemplate> Create(Context context, EmailTemplate e);

		/// <summary>
		/// Updates the database with the given emailTemplate data. It must have an ID set.
		/// </summary>
		Task<EmailTemplate> Update(Context context, EmailTemplate e);

		/// <summary>
		/// List a filtered set of emailTemplates.
		/// </summary>
		/// <returns></returns>
		Task<List<EmailTemplate>> List(Context context, Filter<EmailTemplate> filter);

		/// <summary>
		/// Sends the given email to the given list of recipients, and explicitly waits for it to send.
		/// Generally recommended to use Send instead.
		/// </summary>
		/// <param name="recipients"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		Task<bool> SendAsync(IList<Recipient> recipients, string key);

		/// <summary>
		/// Sends emails to the given recipients without waiting for it to complete.
		/// </summary>
		/// <param name="recipients"></param>
		/// <param name="key"></param>
		void Send(IList<Recipient> recipients, string key);
	}
}
