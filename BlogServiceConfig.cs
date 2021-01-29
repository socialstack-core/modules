using Api.Configuration;

namespace Api.Blogs
{
    public class BlogServiceConfig : Config
    {
        public bool UniqueSlugs { get; set; }
    }
}
