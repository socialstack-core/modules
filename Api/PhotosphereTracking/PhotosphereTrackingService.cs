using Api.Eventing;
using Api.Contexts;
using Api.WebSockets;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Api.PhotosphereTracking;
using Api.Permissions;
using System.Collections.Generic;


namespace Api.PhotosphereTracking
{
    /// <summary>
    /// Handles videos.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class PhotosphereTrackingService : AutoService<PhotosphereTrack, uint> 
    {
        /// <summary>
        /// Constructor sets up listening on page presence
        /// </summary>
        public PhotosphereTrackingService() : base(Events.PhotosphereTrack)
        {
            Cache();

            /*
            Events.PhotosphereTrack.AfterList.AddEventListener(async (Context context, List<PhotosphereTrack> list) =>
            {
                return list;
            });
            */

            Events.WebSocket.Disconnected.AddEventListener(async (Context ctx, WebSocketClient client) =>
            {

                // Triggers when the user login state changes *locally*.
                if (client == null)
                {
                    return client;
                }

                if (client != null && client.PhotosphereTrack != null)
                {
                    await Delete(ctx, client.PhotosphereTrack, DataOptions.IgnorePermissions);
                }

                return client;
            });

            Events.WebSocketMessage.AddEventListener(async (Context context, JObject message, WebSocketClient client, string type) =>
            {

                if (type != "PhotosphereTrack")
                {
                    return message;
                }

                var xObj = message["x"];
                var yObj = message["y"];
                var zObj = message["z"];
                var rotXObj = message["rx"];
                var rotYObj = message["ry"];
                var urlObj = message["url"];

                if (xObj == null || yObj == null || zObj == null || rotXObj == null 
                    || rotYObj == null || urlObj == null)
                {
                    return message;
                }

                double posX = xObj.Value<double>();
                double posY = yObj.Value<double>();
                double posZ = zObj.Value<double>();
                double rotX = rotXObj.Value<double>();
                double rotY = rotYObj.Value<double>();
                string url = urlObj.Value<string>();

                if (client.PhotosphereTrack == null)
                {
                    // We need to create a photosphere track.
                    var position = new PhotosphereTrack()
                    {
                        PosX = posX,
                        PosY = posY,
                        PosZ = posZ,
                        RotationX = rotX,
                        RotationY = rotY,
                        Url = url,
                        UserId = context.UserId
                    };
                    client.PhotosphereTrack = position;
                    _ = Create(context, position);
                }
                else if(client.PhotosphereTrack.Url != url)
                {
                    // Delete this one.
                    _ = Delete(context, client.PhotosphereTrack);

                    // We need to create a photosphere track.
                    var position = new PhotosphereTrack()
                    {
                        PosX = posX,
                        PosY = posY,
                        PosZ = posZ,
                        RotationX = rotX,
                        RotationY = rotY,
                        Url = url,
                        UserId = context.UserId
                    };
                    client.PhotosphereTrack = position;
                    _ = Create(context, position);
                }
                else
                {
                    // Let's update our clients current position.
                    var psTrack = client.PhotosphereTrack;
                    await StartUpdate(context, psTrack, DataOptions.IgnorePermissions); // Ignore perms - if they can make it, they can update it.

                    psTrack.PosX = posX;
                    psTrack.PosY = posY;
                    psTrack.PosZ = posZ;
                    psTrack.RotationX = rotX;
                    psTrack.RotationY = rotY;
                    psTrack.Url = url;

                    await FinishUpdate(context, psTrack);
                }

                return message;
            });

        }
    }
}

namespace Api.WebSockets
{
    /// <summary>
    /// A connected websocket client.
    /// </summary>
    public partial class WebSocketClient
    {
        /// <summary>
        /// This current WS clients photosphere track record. 
        /// </summary>
        public PhotosphereTrack PhotosphereTrack;
    }
}
