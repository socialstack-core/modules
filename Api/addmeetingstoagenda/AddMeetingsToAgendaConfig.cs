using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.UserAgendaEntries
{
    /// <summary>
	/// The appsettings.json config block for push AddMeetingToAgendaConfig config.
	/// </summary>
    public class AddMeetingsToAgendaConfig
    {
        /// <summary>
        /// Set this to true if Collisions are allowed when Adding meetings to the the agenda. 
        /// </summary>
        public bool AllowCollisions { get; set; }
    }
}
