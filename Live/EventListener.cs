using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Contexts;
using System.Threading;
using Api.WebSockets;
using Api.Eventing;


namespace Api.Answers
{

	/// <summary>
	/// Hooks up live websocket support for answers.
	/// Separate such that the live feature can be disabled by just deleting this file.
	/// </summary>
	[EventListener]
	public class LiveEventListener
	{
		
		private IWebSocketService _websocketService;
		
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public LiveEventListener()
		{
			Events.Answer.AfterCreate.AddEventListener(async (Context context, Answer answer) => {

				if (_websocketService == null)
				{
					_websocketService = Services.Get<IWebSocketService>();
				}

				// Send via the websocket service:
				await _websocketService.Send(
					new WebSocketMessage<Answer>() {
						Type = "AnswerCreate?QuestionId=" + answer.QuestionId,
						Entity = answer
					}
				);

				return answer;
			}, 20);
		}

	}
}
