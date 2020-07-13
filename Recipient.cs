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
		public int UserId;

		/// <summary>
		/// Context to send to. Locale comes from this, as well as the user to send to *if* Email isn't also set.
		/// </summary>
		public Context Context;
		
		/// <summary>
		/// Optional custom data fields to use in rendering the email.
		/// </summary>
		public CanvasContext CustomData;

		
		/// <summary>
		/// Gets or sets custom data to pass through to the template rendering process for this recipient.
		/// </summary>
		/// <param name="customDataKey"></param>
		/// <returns></returns>
		public object this[string customDataKey]
		{
			get {
				if (CustomData == null)
				{
					return null;
				}
				return CustomData[customDataKey];
			}
			set {
				if (CustomData == null)
				{
					CustomData = new CanvasContext();
				}
				CustomData[customDataKey] = value;
			}
		}

		/// <summary>
		/// Email recipient for the user and locale defined in the context.
		/// </summary>
		/// <param name="contextForUserAndLocale"></param>
		public Recipient(Context contextForUserAndLocale)
		{
			Context = contextForUserAndLocale;
		}

		/// <summary>
		/// Emails the given user, using the given optional locale. Uses their last seen locale otherwise.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="localeId"></param>
		public Recipient(User user, int localeId = 0)
		{
			User = user;
			UserId = user.Id;
			Context = new Context()
			{
				LocaleId = localeId
			};
		}

		/// <summary>
		/// Emails the given user ID, using the given optional locale. Uses their last seen locale otherwise.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="localeId"></param>
		public Recipient(int userId, int localeId = 0)
		{
			UserId = userId;
			Context = new Context()
			{
				LocaleId = localeId
			};
		}

	}
	
}