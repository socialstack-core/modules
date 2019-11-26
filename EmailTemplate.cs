using Api.Database;


namespace Api.Emails
{
    /// <summary>
    /// A particular email template.
    /// </summary>
    public class EmailTemplate : DatabaseRow
    {
		/// <summary>
		/// The internal key for this token. E.g. "forgot_password".
		/// </summary>
		public string Key;

		/// <summary>
		/// The canvas JSON for this email. This also outputs the emails subject too (as the document title).
		/// </summary>
		public string BodyJson;

		/// <summary>
		/// The notes for this email.
		/// </summary>
		public string Notes;

		/// <summary>
		/// The optional key in the email account config for the account to send these emails as.
		/// </summary>
		public string SendFrom;
    }

}
