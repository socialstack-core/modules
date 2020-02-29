using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Users
{
	/// <summary>
	/// Manages user accounts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public interface IUserService
	{
		/// <summary>
		/// Get a user by the given ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<User> Get(Context context, int id);

		/// <summary>
		/// Get a public facing user profile.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<UserProfile> GetProfile(Context context, int id);
		
		/// <summary>
		/// Gets a public facing user profile.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		Task<UserProfile> GetProfile(Context context, User result);

		/// <summary>
		/// Gets a user by the given email address or username.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="emailOrUsername"></param>
		/// <returns></returns>
		Task<User> Get(Context context, string emailOrUsername);

		/// <summary>
		/// Gets a user by the given username.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="username"></param>
		/// <returns></returns>
		Task<User> GetByUsername(Context context, string username);

		/// <summary>
		/// Gets a user by the given email address.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="email"></param>
		/// <returns></returns>
		Task<User> GetByEmail(Context context, string email);

		/// <summary>
		/// Deletes a user by their ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Creates a new user.
		/// </summary>
		Task<User> Create(Context context, User user);

		/// <summary>
		/// Updates the given user.
		/// </summary>
		Task<User> Update(Context context, User user);

		/// <summary>
		/// Updates a fileref for the given user.
		/// </summary>
		Task<bool> UpdateFile(Context context, int id, string uploadType, string uploadRef);

		/// <summary>
		/// List a filtered set of users.
		/// </summary>
		/// <returns></returns>
		Task<List<User>> List(Context context, Filter<User> filter);


		/// <summary>
		/// Attempt to auth a user now. If successful, returns an auth token to use.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		Task<LoginResult> Authenticate(Context context, UserLogin body);

	}
}
