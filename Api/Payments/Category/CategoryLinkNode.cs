namespace Api.Payments
{
    /// <summary>
    /// A lightweight category node containing only the information required for rendering UI navigation links.
    /// </summary>
    public class CategoryLinkNode
    {
        /// <summary>
        /// The unique ID of the category.
        /// </summary>
        public uint Id;

        /// <summary>
        /// The localized fallback name of the category (typically English).
        /// </summary>
        public string Name;
        
        /// <summary>
        /// The computed primary URL for the category.
        /// </summary>
        public string PrimaryUrl;
        
        /// <summary>
        /// True if this node contains children, preventing a drill-down into empty lists.
        /// </summary>
        public bool HasChildren;
    }
}
