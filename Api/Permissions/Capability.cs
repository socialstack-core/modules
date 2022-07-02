using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using Api.Contexts;
using System.Threading.Tasks;

namespace Api.Permissions
{
    /// <summary>
    /// A particular capability. Functionality asks if capabilities are granted or not.
    /// Modules can define capabilities via simply extending the Capabilities class.
    /// </summary>
    public class Capability
    {
        /// <summary>
        /// Current max assigned ID.
        /// </summary>
        private static int _CurrentId = 0;

        /// <summary>
        /// Current max assigned ID.
        /// </summary>
        public static int MaxCapId => _CurrentId;

        /// <summary>
        /// Capability string name. Of the form "lead_create". Always lowercase.
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Just the feature. Of the form "create". Always lowercase.
        /// </summary>
        public string Feature = "";
        /// <summary>
        /// An index for high speed capability lookups within roles. 
        /// Not consistent across runs - don't store in the database. Use Name instead.
        /// </summary>
        public readonly int InternalId;

        /// <summary>
        /// The content type that this cap came from (if any).
        /// </summary>
        public Type ContentType;

        /// <summary>
        /// The service that it's on.
        /// </summary>
        public AutoService Service;

        /// <summary>
        /// Create a new capability.
        /// </summary>
        /// <param name ="service"></param>
        /// <param name="feature">
        /// Just the feature name, e.g. "List" or "Create".
        /// </param>
        public Capability(AutoService service, string feature = "")
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature), "Capabilities require a feature name.");
            }

            Service = service;
            ContentType = service.ServicedType;
            Feature = feature.ToLower();
            Name = ContentType == null ? Feature : ContentType.Name.ToLower() + "_" + Feature;
            InternalId = _CurrentId++;
        }

    }
}
