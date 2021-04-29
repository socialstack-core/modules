using System.Collections.Generic;
using Api.WebSockets;

namespace Api.PhotosphereTracking
{
    /// <summary>
    /// A class for WS message
    /// </summary>
    public class PhotosphereTrackingWsMessage: WebSocketMessage
    {
        /// <summary>
        /// 
        /// </summary>
        public PhotosphereTrackingWsMessage() {
            Type = "PhotosphereTrack";
        }

        /// <summary>
        /// A list of positions, currently this is everyone on a page, however it would eventually be people most close to you in the network graph
        /// </summary>
        public List<PhotosphereTrackingUserUpdate> Positions;
    }
}