using Api.CanvasRenderer;
using Api.Contexts;
using Api.Users;

namespace Api.Emails{
	
	/// <summary>
	/// A recipient of an email.
	/// </summary>
	public partial class Recipient{

		/// <summary>
		/// User if there is one.
		/// </summary>
		public User User;

		/// <summary>
		/// User ID, if there is one.
		/// </summary>
		public uint UserId;

		/// <summary>
		/// Context to send to. Locale comes from this, as well as the user to send to *if* Email isn't also set.
		/// </summary>
		public Context Context;
		
		/// <summary>
		/// Optional custom data to use in rendering the email. This arrives as the primary object inside react 
		/// (Use either &lt;Content primary&gt; or Content.getPrimary(this.context) to access it).
		/// </summary>
		public object CustomData;

		/// <summary>
		/// Just their email address if this email is not targeted at a user.
		/// </summary>
		public string EmailAddress;

		/// <summary>
		/// Email recipient for the user and locale defined in the context.
		/// </summary>
		/// <param name="contextForUserAndLocale"></param>
		public Recipient(Context contextForUserAndLocale)
		{
			Context = contextForUserAndLocale;

			if (Context != null)
			{
				User = Context.User;
				UserId = Context.UserId;
			}
		}

		/// <summary>
		/// Emails the given user, using the given optional locale. Uses their last seen locale otherwise.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="localeId"></param>
		public Recipient(User user, uint localeId = 0)
		{
			User = user;
			UserId = user.Id;
			Context = new Context(localeId, user, user.Role)
			{
				LocaleId = localeId
			};
		}

		/// <summary>
		/// Emails the given user ID, using the given optional locale. Uses their last seen locale otherwise.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="localeId"></param>
		public Recipient(uint userId, uint localeId = 0)
		{
			UserId = userId;
			Context = new Context(localeId, userId, 4);
		}

		/// <summary>
		/// Emails the given address.
		/// </summary>
		/// <param name="emailAddress"></param>
		/// <param name="localeContext"></param>
		public Recipient(string emailAddress, Context localeContext)
		{
			EmailAddress = emailAddress;
			Context = localeContext;
		}

		/// <summary>
		/// Emails the given address.
		/// </summary>
		/// <param name="emailAddress"></param>
		/// <param name="localeId"></param>
		public Recipient(string emailAddress, uint localeId = 0)
		{
			EmailAddress = emailAddress;
			Context = new Context()
			{
				LocaleId = localeId
			};
		}

	}

}