using Api.Database;
using Api.Translate;
using Api.Users;

namespace Api.Emails
{
    /// <summary>
    /// A particular email template.
    /// </summary>
    public class EmailTemplate : RevisionRow
    {
		/// <summary>
		/// The internal key for this token. E.g. "forgot_password".
		/// </summary>
		[DatabaseIndex]
		[DatabaseField(Length = 80)]
		public string Key;

		/// <summary>
		/// The internal name for this template.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Name;
		
		/// <summary>
		/// Email subject - can be overriden by document title when rendering the body.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Localized]
		public string Subject;
		
		/// <summary>
		/// The canvas JSON for this email. This also outputs the emails subject too (as the document title).
		/// </summary>
		[Localized]
		public string BodyJson;

		/// <summary>
		/// The notes for this email.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Notes;

		/// <summary>
		/// The optional key in the email account config for the account to send these emails as.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string SendFrom;

		/// <summary>
		/// The type of email. 1=Essential, 2=Marketing. Used for opting out of groups of email. 
		/// These numbers MUST go up in powers of 2, as the opt-out system is flag based.
		/// </summary>
		public int EmailType = 1;
    }

}
