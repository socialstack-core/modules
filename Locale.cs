using Api.Users;


namespace Api.Translate
{
    /// <summary>
    /// </summary>
    public partial class Locale : VersionedContent<uint>
	{
		/// <summary>
		/// The name.
		/// </summary>
		[Localized]
		public string Name;

		/// <summary>
		/// Usually a 5 letter locale code e.g. "en_GB". May also be just 2 e.g. "fr".
		/// </summary>
		public string Code;
    }

}
