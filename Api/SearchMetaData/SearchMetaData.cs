namespace Api.SearchMetaData
{
    /// <summary>
    /// A structure representing search metadata
    /// </summary>
    public class SearchMetaData
    {
        /// <summary>
        /// The target object
        /// </summary>
        public object PrimaryObject { get; set; }
        
        /// <summary>
        /// Any included meta data.
        /// </summary>
        public MetaDataInclude[] Includes { get; set; }

    }
    
    /// <summary>
    /// A structure representing a meta data include.
    /// </summary>
    public class MetaDataInclude
    {
        /// <summary>
        /// The name of the included
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The target field
        /// </summary>
        public string Field { get; set; }
        
        /// <summary>
        /// Any potential values.
        /// </summary>
        public dynamic[] Values { get; set; }
    }

}
