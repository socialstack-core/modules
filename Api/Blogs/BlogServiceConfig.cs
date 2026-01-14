using Api.Configuration;

namespace Api.Blogs
{
    /// <summary>
    /// Configurations used by the Blog Service.
    /// </summary>
    public class BlogServiceConfig : Config
    {
        /// <summary>
        /// Determines if slugs need to be unique.
        /// </summary>
        public bool UniqueSlugs { get; set; } = true;

		/// <summary>
		/// Determines if slugs are generated on creation/updates if none are provided.
		/// </summary>
		public bool GenerateSlugs { get; set; } = true;

        /// <summary>
        /// Determines if synopsis are generated on creation/updates if none are provided.
        /// </summary>
        public bool GenerateSynopsis { get; set; } = true;
		
        /// <summary>
        /// True if the site has multiple blogs on it.
        /// </summary>
        public bool MultipleBlogs { get; set; }
    }
}
