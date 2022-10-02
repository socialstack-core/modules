namespace Api.Translate
{
    /// <summary>
    /// Wrapper class for po data translations
    /// </summary>
    public class PoTranslation
    {
        /// <summary>
        /// Content type
        /// </summary>
        public string ContentType { set; get; }
        /// <summary>
        /// Content ID
        /// </summary>
        public string ContentId { set; get; }
        /// <summary>
        /// Entry ID
        /// </summary>
        public ulong Id { set; get; }
        /// <summary>
        /// Field name
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// Original text
        /// </summary>
        public string Original { get; set; }
        /// <summary>
        /// Translated text
        /// </summary>
        public string Translated { get; set; }

    }
}
