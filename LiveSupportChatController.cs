using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Api.Contexts;
using Api.Permissions;
using System.IO.Compression;
using System.IO;
using Api.Startup;
using System.Globalization;
using CsvHelper;

namespace Api.LiveSupportChats
{
    /// <summary>Handles liveSupportChat endpoints.</summary>
    [Route("v1/liveSupportChat")]
    public partial class LiveSupportChatController : AutoController<LiveSupportChat>
    {
        private LiveSupportMessageService _liveChatMessages;
        private CsvMapping _csvMapping;

        /// <summary>
        /// GET /v1/livesupportchat/grouplist
        /// Gets all chats from a given start time and returns them as CSV's in a zip file.
        /// </summary>
        /// <returns></returns>
        [HttpGet("grouplist")]
        public virtual async Task<FileResult> GroupList()
        {
            return await GroupList(null);
        }

        /// <summary>
        /// POST /v1/livesupportchat/grouplist
        /// </summary>
        /// Gets all chats from a given start time 
        /// <returns></returns>
        [HttpPost("grouplist")]
        public async Task<FileResult> GroupList([FromBody] JObject filters)
        {
            var context = Request.GetContext();

            // We need to get the chats within the given time range as provided by the filter.
            var chats = await _service.List(context, new Filter<LiveSupportChat>(filters));

            if (chats.Count <= 0)
            {
                return null;
            }

            // We need to list the chat messages
            if (_liveChatMessages == null)
            {
                _liveChatMessages = Services.Get<LiveSupportMessageService>();
            }

            if (_csvMapping == null)
            {
                _csvMapping = new CsvMapping(typeof(LiveSupportMessage));
            }
            
            var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // We need to get the corresponding csv for each chat we received.
                foreach (var chat in chats)
                {
                    // Start by grabbing all messages from the chat.
                    var messages = await _liveChatMessages.List(context, new Filter<LiveSupportMessage>().Equals("LiveSupportChatId", chat.Id));

                    if (messages.Count > 0)
                    {
                        var csvFile = archive.CreateEntry("Chat#" + chat.Id + ".csv");

                        using (var entryStream = csvFile.Open())
                        using (var streamWriter = new StreamWriter(entryStream, System.Text.Encoding.UTF8, -1, true))
                        {
                            using (var csv = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                            {
                                foreach (var field in _csvMapping.Entries)
                                {
                                    csv.WriteField(field.Name);
                                }

                                foreach (var row in messages)
                                {
                                    csv.NextRecord();

                                    foreach (var field in _csvMapping.Entries)
                                    {
                                        csv.WriteField(field.GetValue(row));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            return File(memoryStream, "application/zip", "livesupportchat.zip");
        }
    }
}
