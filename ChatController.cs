using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using System;
using System.Collections.Generic;
using Api.Users;

namespace Api.Chats
{
    /// <summary>Handles chat endpoints.</summary>
    [Route("v1/chat")]
	public partial class ChatController : AutoController<Chat>
    {
        private UserService _userService;

        /*
        [HttpPost("loadbyusers")]
        public async Task<Chat> LoadByUsers([FromBody] uint[] ids)
        {
            var context = await Request.GetContext();

            // Let's get the chat with the permitted users in it.
            var chat = await _service.Where("UserPermits = [?]").Bind(ids).Last(context);

            // Does the chat not exist and are we are creating it if it doesn't exist?
            if(chat == null)
            {
                chat = await _service.Create(context, new Chat() { }, DataOptions.IgnorePermissions);
                var userPemits = await _service.GetMap<User, uint>("UserPermits");

                await userPemits.EnsureMapping(context, chat.Id, ids);
            }
            return chat;
        }*/
    }
}