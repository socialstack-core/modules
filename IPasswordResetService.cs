using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;


namespace Api.PasswordReset
{
	/// <summary>
	/// Manages password reset requests.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public interface IPasswordResetService
	{
		/// <summary>
		/// Sends a password reset email for the given user.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="toEmail"></param>
		/// <returns></returns>
		Task<bool> Create(int userId, string toEmail);

		/// <summary>
		/// Get a reset request by the given ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<PasswordResetRequest> Get(int id);
	}
}
