using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Startup;

namespace Api.Pages
{
    public partial class Page
    {
        /// <summary>
        /// The image which will represent the page within Open Graph.
        /// </summary>
        [Meta("image")]
        public string OpenGraphImageRef;

        /// <summary>
        /// If a page needs a specified OpenGraph Type beyond "website"
        /// </summary>
        [Meta("type")]
        public string OpenGraphType;

        /// <summary>
        /// If a page needs a specified OpenGraph Description beyond default website description
        /// </summary>
        [Meta("description")]
        public string OpenGraphDescription;
    }
}
