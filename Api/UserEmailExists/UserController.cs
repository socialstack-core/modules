using Api.Contexts;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Users;


public partial class UserController
{
	
	/// <summary>
	/// True/ false endpoint if an email exists.
	/// Optional as it leaks a little information. Only install if you absolutely need it.
	/// </summary>
	[HttpPost("email-exists")]
	public async ValueTask<EmailExistsResult> EmailExists([FromBody] EmailExistsBody body)
	{
		var context = await Request.GetContext();
		var result = await (_service as UserService).EmailExists(context, body == null ? null : body.Email);
		return new EmailExistsResult(){ Registered = result };
	}
	
}

/// <summary>
/// Indicates if an email addr is already registered.
/// </summary>
public class EmailExistsBody
{
	/// <summary>
	/// The email addr to check.
	/// </summary>
	public string Email;
}

/// <summary>
/// Indicates if an email addr is already registered.
/// </summary>
public class EmailExistsResult
{
	/// <summary>
	/// True if yes.
	/// </summary>
	public bool Registered;
}