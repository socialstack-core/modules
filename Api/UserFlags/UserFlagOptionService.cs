using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Eventing;

namespace Api.UserFlags
{
    /// <summary>
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public class UserFlagOptionService : AutoService<UserFlagOption>
    {
        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public UserFlagOptionService() : base(Events.UserFlagOption)
        {
            // Create admin pages if they don't already exist:
            InstallAdminPages("User Flag Options", "fa:fa-flag", new string[] { "id", "bodyJson" });
        }
    }
}
