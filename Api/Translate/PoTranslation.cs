namespace Api.Translate
{
    /// <summary>
    /// Wrapper class for po data translations
    /// </summary>
    public class PoTranslation
    {
        public string ContentType { set; get; }
        public string ContentId { set; get; }
        public ulong Id { set; get; }
        public string FieldName { get; set; }
        public string Original { get; set; }
        public string Translated { get; set; }

    }
}
