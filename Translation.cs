using System.Text;
using Api.Database;


namespace Api.Translate
{
    /// <summary>
    /// Represents a particular translation.
    /// The typical hierarchy of these is e.g:
    /// Locale (en_GB) > ContentTypeId (products) > ContentId (particular product) > Key (product title) > Html (the actual text)
    /// </summary>
    public class Translation : DatabaseRow
    {
		/// <summary>
		/// ID for the locale of this translation.
		/// </summary>
		public int Locale;

		/// <summary>
		/// The content type ID of the content this is a translation for. See also: Api.Database.ContentTypes
		/// </summary>
		public int ContentTypeId;

		/// <summary>
		/// The ID of the content this is a translation for.
		/// </summary>
		public int ContentId;

		/// <summary>
		/// The translation key - e.g. "name" or "body" (referring to product name).
		/// </summary>
		public string Key;

		/// <summary>
		/// The HTML supporting text itself
		/// </summary>
		public string Html;


        /// <summary>
        /// This translations hierachy key - these are essentially unique ways to recognise a particular translation.
        /// </summary>
        public string HierarchyKey
        {
            get
            {
                // Note: ContentId is always used, including when it's (often) 0.
                return Locale + "/" + ContentTypeId + "/" + ContentId + "/" + Key;
            }
        }

        /// <summary>
        /// Converts this translation to the PO format.
        /// Usually called on the en_US translation, and given a translation from some other locale.
        /// </summary>
        /// <param name="localeSpecific"></param>
        /// <param name="builder"></param>
        public void ToPo(Translation localeSpecific, StringBuilder builder)
        {
            // ID line - e.g. #: 14 - products>1>title
            builder.Append("#: " + Id + "\r\n");

            builder.Append("msgctxt \"");
			builder.Append(ContentTypes.GetName(ContentTypeId));
			builder.Append(">" + ContentId + ">" + Key);
            builder.Append("\"\r\n");

            // Msgid is the HTML:
            builder.Append("msgid \"");
            
            // Any double quotes are also escaped.
            builder.Append(EscapeForPo(Html));

            // The final quote to close msgid:
            builder.Append("\"\r\n");

            // Next, either append a blank msgstr or a value depending on if we've got a localeSpecific object:
            builder.Append("msgstr \"");

            if (localeSpecific != null)
            {
                builder.Append(EscapeForPo(localeSpecific.Html));
            }

            builder.Append("\"\r\n\r\n");

        }

        /// <summary>
        /// Escapes a value for use in PO/POT files.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string EscapeForPo(string value)
        {
            if (value == null)
            {
                return "";
            }

            // Newlines are escaped by making them literal as well as surrounding the line in quotes.
            // Hello
            // world

            // becomes
            // "Hello\n"
            // "world"

            return value.Replace("\"", "\\\"").Replace("\r\n", "\n").Replace("\n", "\\n\"\n\"");
        }

        /// <summary>
        /// Writes this translation to the POT file format - a common interchange format for translations.
        /// </summary>
        /// <param name="builder">Appends the POT lines here.</param>
        public void ToPot(StringBuilder builder)
        {
            // A POT line is the same as a PO but just without the locale specific translation
            ToPo(null, builder);
        }
    }

}
