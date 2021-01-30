using Api.Configuration;

namespace Api.Blogs
{
    public class BlogServiceConfig : Config
    {
        /// <summary>
        /// Determines is slugs need to be unique.
        /// </summary>
        public bool UniqueSlugs { get; set; }

        /// <summary>
        /// Determines if slugs are generated on creation if none are provided.
        /// </summary>
        public bool GenerateSlugs { get; set; }
    }
}
