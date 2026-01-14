
using Api.Configuration;
using System.Collections.Generic;

namespace Api.Swagger
{
    /// <summary>
    /// Config for swagger integration and controller mapping
    /// </summary>
    public class SwaggerConfig : Config
    {

        /// <summary>
        /// Overide the title in the swagger rendering
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Overide the description in the swagger rendering
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Overide the version in the swagger rendering
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///  Excluded endpoint types csv, pot etc 
        /// </summary>
        public List<string> ExcludedEndpointTypes { get; set; } = new List<string>();

        /// <summary>
        /// Excluded controllers (list oif service names to exclude)
        /// </summary>
        public List<string> ExcludedServices { get; set; } = new List<string>();

        /// <summary>
        /// Excluded operations (list of httpoperations POST, DELETE etc to exclude)
        /// </summary>
        public List<string> ExcludedOperations { get; set; } = new List<string>();

        /// <summary>
        /// Included controllers (list of service names to include, if set only services in this list will be exposed)
        /// </summary>
        public List<string> IncludedServices { get; set; } = new List<string>();

        /// <summary>
        /// Exclude the schema section
        /// </summary>
        public bool ExcludeSchema { get; set; } = true;

    }

}