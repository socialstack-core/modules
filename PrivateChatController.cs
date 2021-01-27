using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Database;
using Api.Users;
using Api.Startup;
using Api.PrivateChats;
using System.Collections.Generic;

namespace Api.PrivateChats
{
    /// <summary>Handles privateChat endpoints.</summary>
    [Route("v1/privateChat")]
	public partial class PrivateChatController : AutoController<PrivateChat>
    {
        private PrivateChatService _chatService;
        private PermittedContentService _permittedContentService;
        private UserService _userService;
        private int userContentTypeId = ContentTypes.GetId(typeof(User));
        private int privateChatContentTypeId = ContentTypes.GetId(typeof(PrivateChat));

        /// <summary>
        /// Used to load a one to one chat between the invoking user and the id of the user provided. 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}/loadbyuser")]
        public async Task<PrivateChat> LoadByUser(int id)
        {
            var context = Request.GetContext();
            var secondUserId = id;
            var userId = context.UserId;
            var privateChats = await _service.List(context, new Filter<PrivateChat>());
            var results = new List<PrivateChat>();
            foreach (var privateChat in privateChats)
            {
                // This is literally all of them atm
                if (privateChat == null || privateChat.PermittedUsers == null || privateChat.PermittedUsers.Count != 2)
                {
                    continue;
                }
                // 2 permitted users, is it the chat we want?
                var permitA = privateChat.PermittedUsers[0];
                var permitB = privateChat.PermittedUsers[1];
                if ((permitA.PermittedContentId == userId && permitB.PermittedContentId == secondUserId) || (permitB.PermittedContentId == userId && permitA.PermittedContentId == secondUserId))
                {
                    // match! do something with private chat.
                    results.Add(privateChat);
                }
            }

            // Do we have any results?
            if (results.Count == 0)
            {
                return null;
            }
            else if (results.Count == 1)
            {
                return results[0];
            }
            else
            {
                results.Sort((x, y) => x.CreatedUtc.CompareTo(y.CreatedUtc));

                return results[0];
            }
        }
    }
}