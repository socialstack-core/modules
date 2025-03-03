using Api.Layouts;
using Api.Startup;
using Api.Users;

namespace Api.Pages
{
    [HasVirtualField("LayoutId", typeof(Layout), "Id")]
    public partial class Page : VersionedContent<uint> 
    {
        /// <summary>
        /// The chosen layout
        /// </summary>
        public uint LayoutId = 0;
    }
}