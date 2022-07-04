using Api.Configuration;

namespace Api.OpenGraphTags
{
    /// <summary>
    /// Configurations used by the OpenGraphTagService.
    /// </summary>
    public class OpenGraphTagsServiceConfig : Config
    {
        /// <summary>
        /// The site name used in the OpenGraph site_name tag.
        /// </summary>
        public string SiteName { get; set; }

        /// <summary>
        /// The page's description if primary object or the page's description isn't set.
        /// </summary>
        public string DefaultDescription { get; set; }
    }
}
