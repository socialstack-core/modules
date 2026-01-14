using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;

namespace Api.Emails
{
    /// <summary>
    /// A particular email template.
    /// </summary>
    public class EmailTemplate : VersionedContent<uint>
	{
		/// <summary>
		/// The internal key for this token. E.g. "forgot_password".
		/// </summary>
		[DatabaseIndex]
		[DatabaseField(Length = 80)]
		[Data("required", true)]
		[Data("validate", "Required")]
		public string Key;

		/// <summary>
		/// The internal name for this template.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Data("required", true)]
		[Data("validate", "Required")]
		public string Name;
		
		/// <summary>
		/// Email subject - can be overriden by document title when rendering the body.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Data("required", true)]
		[Data("validate", "Required")]
		public Localized<string> Subject;
		
		/// <summary>
		/// The canvas JSON for this email. This also outputs the emails subject too (as the document title).
		/// </summary>
		[Data("required", true)]
		[Data("validate", "Required")]
		[Data("tab", "design")]
		public Localized<JsonString> BodyJson;

		/// <summary>
		/// The name of the primary content type if there is one (and it's actually a content type).
		/// </summary>
		public string PrimaryContentType;

		/// <summary>
		/// If the email template uses a content type for its primary content, this is the includes string to also use with it.
		/// Much the same as how it works for a page too.
		/// </summary>
		public string PrimaryContentIncludes;

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
