using System;
using System.Threading.Tasks;
using Api.Database;
using Api.Emails;
using Microsoft.AspNetCore.Http;
using Api.Contexts;


namespace Api.PasswordReset
{

	/// <summary>
	/// Manages password reset requests.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public class PasswordResetService : IPasswordResetService
	{
        private IDatabaseService _database;
        private IEmailService _email;
		private readonly Query<PasswordResetRequest> createQuery;
		private readonly Query<PasswordResetRequest> selectQuery;
		private readonly Query<PasswordResetRequest> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PasswordResetService(IDatabaseService database, IEmailService email)
        {
            _database = database;
            _email = email;
			createQuery = Query.Insert<PasswordResetRequest>();
			updateQuery = Query.Update<PasswordResetRequest>();
			selectQuery = Query.Select<PasswordResetRequest>();
		}
		
		/// <summary>
		/// Get a reset request by the given ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
        public async Task<PasswordResetRequest> Get(int id)
        {
			return  await _database.Select(selectQuery, id);
        }

        /// <summary>
        /// Sends a password reset email for the given user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="toEmail"></param>
        /// <returns></returns>
        public async Task<bool> Create(int userId, string toEmail)
        {
            // Generate a random token:
            var token = RandomToken.Generate(40);
            var expiry = DateTime.UtcNow.AddDays(1);

			var req = new PasswordResetRequest();
			req.Token = token;
			req.ExpiryUtc = expiry;
			req.UserId = userId;

			await _database.Run(createQuery, req);

			/*
            // Send the email now:
            dynamic emailContext = new EmailContext();
            emailContext["Token"] = token;
            emailContext["ResetUrl"] = _email.ResolveUrl("/en-admin/reset/" + token + "/");

            await _email.SendTemplate(toEmail, "ForgotPassword", emailContext);
			*/
			
            return true;
        }
		
	}
}
