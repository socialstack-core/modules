using Api.Database;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.LiveSupportChats;
using Api.Startup;
using System;
using Api.MeetingAppointments;
using Api.ExpertQuestions;
using Api.LiveChatHours;

namespace Api.ChatBotSimple
{
	/// <summary>
	/// Handles chatBotDecisions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ChatBotDecisionService : AutoService<ChatBotDecision>
    {
		private Dictionary<int, List<ChatBotDecision>> _inReplyToMap;
		private LiveSupportMessageService _liveChatMessages;
		private LiveSupportChatService _liveChat;
		private MeetingAppointmentService _meetingsAppointments;
		private ExpertQuestionService _expertQuestions;
		private LiveChatHourService _liveChatHours;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ChatBotDecisionService(LiveSupportMessageService liveChatMessages) : base(Events.ChatBotDecision)
        {
			_liveChatMessages = liveChatMessages;
			
			InstallAdminPages("ChatBot Decisions", "fa:fa-rocket", new string[] { "id", "messageText" });
			Cache();

			Events.LiveSupportChat.AfterCreate.AddEventListener(async(Context ctx, LiveSupportChat chat) => {
				
				if (chat == null || _inReplyToMap == null)
				{
					return chat;
				}
				
				List<ChatBotDecision> list = null;
				if(_inReplyToMap.TryGetValue(0, out list) && list.Count > 0)
				{
					// Let's check if this is a special mode. If not, send the standard chat first.
					if(chat.Mode.HasValue)
                    {
						foreach(var dec in list)
                        {
							if (dec.Mode == chat.Mode)
                            {
								// We have a match, send that message.
								await SendChatBotMessage(ctx, chat.Id, dec);


								// Now, do we need to make a meeting entry or ask an expert entry?
								if(chat.Mode == 1 || chat.Mode == 11)
                                {
									// Also, let's get this started with a meeting entry.
									if (_meetingsAppointments == null)
									{
										_meetingsAppointments = Services.Get<MeetingAppointmentService>();
									}

									if (_liveChat == null)
									{
										_liveChat = Services.Get<LiveSupportChatService>();
									}

									// Let's create a new meeting appointment entry.
									var appt = await _meetingsAppointments.Create(ctx, new MeetingAppointment() { UserId = chat.UserId, FullName = chat.FullName, Email = chat.Email });

									// Let's also set the chat to have the appointment's id.
									chat.MeetingAppointmentId = appt.Id;
									chat = await _liveChat.Update(ctx, chat);
								}

								if (chat.Mode == 2 || chat.Mode == 12)
                                {
									if (_expertQuestions == null)
                                    {
										_expertQuestions = Services.Get<ExpertQuestionService>();
                                    }

									if (_liveChat == null)
									{
										_liveChat = Services.Get<LiveSupportChatService>();
									}

									// Let's create a new expert question entry.
									var quest = await _expertQuestions.Create(ctx, new ExpertQuestion() { UserId = chat.UserId, FullName = chat.FullName, Email = chat.Email });

									// Let's also set the chat to have question id.
									chat.ExpertQuestionId = quest.Id;
									chat = await _liveChat.Update(ctx, chat);
								}
							}
                        }
                    }
					else
                    {
						var lsm = await Get(ctx, list[0].Id);
						// In this case we can only use the first entry.
						await SendChatBotMessage(ctx, chat.Id, lsm);
					}
				}
				
				return chat;
			});
			
			Events.LiveSupportMessage.AfterCreate.AddEventListener(async (Context ctx, LiveSupportMessage message) => {

				if (message == null || _inReplyToMap == null || message.InReplyTo == 0)
				{
					return message;
				}

				// this is the one that sets the name of the user, so let's grab it.
				if (message.MessageType == 3)
				{
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					if(chat == null)
                    {
						// The target chat does not exist.
						return message;
                    }

					chat.FullName = message.Message;
					chat = await _liveChat.Update(ctx, chat);

					// Does this chat have an appointment? if so, let's set it's name.
					// Does the live chat have a meeting? If not, we done.
					if (chat.MeetingAppointmentId.HasValue)
					{
						if (_meetingsAppointments == null)
						{
							_meetingsAppointments = Services.Get<MeetingAppointmentService>();
						}

						// Yep, let's get said meeting appointment
						var appt = await _meetingsAppointments.Get(ctx, chat.MeetingAppointmentId.Value);

						// is the appt valid?
						if (appt != null)
						{
							// Set the name
							appt.FullName = message.Message;
							appt = await _meetingsAppointments.Update(ctx, appt);
						}
					}

					// What about an expert question?
					if (chat.ExpertQuestionId.HasValue)
                    {
						if(_expertQuestions == null)
                        {
							_expertQuestions = Services.Get<ExpertQuestionService>();
                        }

						// Yep, let's get said expert question.
						var exp = await _expertQuestions.Get(ctx, chat.ExpertQuestionId.Value);

						// is the exp valid?
						if (exp != null)
                        {
							// Set the name.
							exp.FullName = message.Message;
							exp = await _expertQuestions.Update(ctx, exp);
                        }
                    }

				}

				// The user is setting the email address for this chat.
				if (message.MessageType == 4)
				{

					// We need to create a meeting Appointment instance - let's grab that service and the live chat service so we can get existing details. 
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					// Let's set the live chat to have the email address.
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);
					chat.Email = message.Message;
					chat = await _liveChat.Update(ctx, chat);

					// Does this chat have an appointment? if so, let's set it's name.
					// Does the live chat have a meeting? If not, we done.
					if (chat.MeetingAppointmentId.HasValue)
					{
						if (_meetingsAppointments == null)
						{
							_meetingsAppointments = Services.Get<MeetingAppointmentService>();
						}

						// Yep, let's get said meeting appointment
						var appt = await _meetingsAppointments.Get(ctx, chat.MeetingAppointmentId.Value);

						// is the appt valid?
						if (appt != null)
						{
							// Set the email
							appt.Email = message.Message;
							appt = await _meetingsAppointments.Update(ctx, appt);
						}
					}

					// What about an expert question?
					if (chat.ExpertQuestionId.HasValue)
                    {
						if (_expertQuestions == null)
                        {
							_expertQuestions = Services.Get<ExpertQuestionService>();
                        }

						// Yep, let's get the expert question.
						var exp = await _expertQuestions.Get(ctx, chat.ExpertQuestionId.Value);

						// is the exp valid?
						if (exp != null)
                        {
							// Set the email
							exp.Email = message.Message;
							exp = await _expertQuestions.Update(ctx, exp);
                        }
                    }
				}

				// Is the user setting the date for the meeting?
				if (message.MessageType == 8)
				{
					// We need to see if there is currently an appointment on this chat instance.
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					if (_meetingsAppointments == null)
					{
						_meetingsAppointments = Services.Get<MeetingAppointmentService>();
					}

					// Let's get the live chat
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					// Does the live chat have a meeting? If not, we done.
					if (chat.MeetingAppointmentId.HasValue)
					{
						// Yep, let's get said meeting appointment
						var appt = await _meetingsAppointments.Get(ctx, chat.MeetingAppointmentId.Value);

						// is the appt valid?
						if (appt != null)
						{
							// Set the new Date
							appt.Date = message.HiddenDatePayload;
							appt = await _meetingsAppointments.Update(ctx, appt);
						}
					}
				}

				// Is the user setting the meeting topic?
				if (message.MessageType == 9)
				{
					// We need to see if there is currently an appointment on this chat instance.
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					if (_meetingsAppointments == null)
					{
						_meetingsAppointments = Services.Get<MeetingAppointmentService>();
					}

					// Let's get the live chat
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					// Does the live chat have a meeting? If not, we done.
					if (chat.MeetingAppointmentId.HasValue)
					{
						// Yep, let's get said meeting appointment
						var appt = await _meetingsAppointments.Get(ctx, chat.MeetingAppointmentId.Value);

						// is the appt valid?
						if (appt != null)
						{
							// Nice, now we can do business - set the phone number
							appt.Topic = message.Message;
							appt = await _meetingsAppointments.Update(ctx, appt);
						}
					}
				}

				// is the user setting the phonenumber for a meeting?
				if (message.MessageType == 5)
				{
					// We need to see if there is currently an appointment on this chat instance.
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					if (_meetingsAppointments == null)
					{
						_meetingsAppointments = Services.Get<MeetingAppointmentService>();
					}

					// Let's get the live chat
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					// Does the live chat have a meeting? If not, we done.
					if (chat.MeetingAppointmentId.HasValue)
					{
						// Yep, let's get said meeting appointment
						var appt = await _meetingsAppointments.Get(ctx, chat.MeetingAppointmentId.Value);

						// is the appt valid?
						if (appt != null)
						{
							// Nice, now we can do business - set the phone number
							appt.ContactNumber = message.Message;
							appt = await _meetingsAppointments.Update(ctx, appt);
						}
					}
				}

				// Let's set the time zone string.
				if (message.MessageType == 11)
				{
					// We need to see if there is currently an appointment on this chat instance.
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					if (_meetingsAppointments == null)
					{
						_meetingsAppointments = Services.Get<MeetingAppointmentService>();
					}

					// Let's get the live chat
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					// Does the live chat have a meeting? If not, we done.
					if (chat.MeetingAppointmentId.HasValue)
					{
						// Yep, let's get said meeting appointment
						var appt = await _meetingsAppointments.Get(ctx, chat.MeetingAppointmentId.Value);

						// is the appt valid?
						if (appt != null)
						{
							// Nice, now we can do business - set the phone number
							appt.Timezone = message.Message;
							appt = await _meetingsAppointments.Update(ctx, appt);
						}
					}
				}

				// We are setting the offering area.
				if (message.MessageType == 12)
				{
					// We need to see if there is currently an appointment on this chat instance.
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					if (_expertQuestions == null)
					{
						_expertQuestions = Services.Get<ExpertQuestionService>();
					}

					// Let's get the live chat
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					// Does the live chat have a meeting? If not, we done.
					if (chat.ExpertQuestionId.HasValue)
					{
						// Yep, let's get said meeting appointment
						var quest = await _expertQuestions.Get(ctx, chat.ExpertQuestionId.Value);

						// is the appt valid?
						if (quest != null)
						{
							// Nice, now we can do business - set the phone number
							quest.OfferingArea = message.Message;
							quest = await _expertQuestions.Update(ctx, quest);
						}
					}
				}

				// We are setting the initiative of a question
				if (message.MessageType == 13)
				{
					// We need to see if there is currently an appointment on this chat instance.
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					if (_expertQuestions == null)
					{
						_expertQuestions = Services.Get<ExpertQuestionService>();
					}

					// Let's get the live chat
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					// Does the live chat have a meeting? If not, we done.
					if (chat.ExpertQuestionId.HasValue)
					{
						// Yep, let's get said meeting appointment
						var quest = await _expertQuestions.Get(ctx, chat.ExpertQuestionId.Value);

						// is the appt valid?
						if (quest != null)
						{
							// Nice, now we can do business - set the phone number
							quest.Initiative = message.Message;
							quest = await _expertQuestions.Update(ctx, quest);
						}
					}
				}

				// We are setting the question's question
				if (message.MessageType == 14)
                {
					// We need to see if there is currently an appointment on this chat instance.
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					if (_expertQuestions == null)
					{
						_expertQuestions = Services.Get<ExpertQuestionService>();
					}

					// Let's get the live chat
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					// Does the live chat have a meeting? If not, we done.
					if (chat.ExpertQuestionId.HasValue)
					{
						// Yep, let's get said meeting appointment
						var quest = await _expertQuestions.Get(ctx, chat.ExpertQuestionId.Value);

						// is the appt valid?
						if (quest != null)
						{
							// Nice, now we can do business - set the question
							quest.Question = message.Message;
							quest = await _expertQuestions.Update(ctx, quest);
						}
					}
				}

				var outOfHoursResponse = false;
				/*
				// The user is attempting to speak to an operator.
				if(message.InReplyTo == 3 && message.Message == "Speak to a live operator")
                {
					// We need to check the hours currently.
					if (_liveChat == null)
                    {
						_liveChat = Services.Get<LiveSupportChatService>();
                    }

					if (_liveChatHours == null)
                    {
						_liveChatHours = Services.Get<LiveChatHourService>();
                    }

					// Let's grab the live chat hours
					var hours = await _liveChatHours.Get(ctx, 1);

					// Is hours null? if so, let's let things carry on.
					if (hours != null)
                    {
						// Let's grab the current time.
						var now = DateTime.UtcNow;
						var startHour = hours.AvailabilityStartUtc.Hour;
						var endHour = startHour + hours.Duration;
						if ((now.Hour >= startHour && now.Hour < endHour) || (now.Hour + 24 >= startHour && now.Hour + 24 < endHour))
                        {
							// We are good!
							outOfHoursResponse = false;
                        }
						else
                        {
							// We are not good, we need to override the response.
							outOfHoursResponse = true;
                        }
					}
                }

				// The user is creating a new meeting instance. 
				if(message.InReplyTo == 3 && message.Message == "Book a meeting")
                {
					// We need to create a meeting Appointment instance - let's grab that service and the live chat service so we can get existing details. 
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					if (_meetingsAppointments == null)
                    {
						_meetingsAppointments = Services.Get<MeetingAppointmentService>();
                    }

					// Let's grab the current live chat.
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					// Let's create a new meeting appointment entry.
					var appt = await _meetingsAppointments.Create(ctx, new MeetingAppointment() { FullName = chat.FullName, Email = chat.Email, UserId = chat.UserId });

					// Let's also set the chat to have the appointment's id.
					chat.MeetingAppointmentId = appt.Id;
					chat = await _liveChat.Update(ctx, chat);
				}

				// The user is creating a new ask an expert entry.
				if(message.InReplyTo == 3 && message.Message == "Ask an expert")
                {
					// We need to create a meeting Appointment instance - let's grab that service and the live chat service so we can get existing details. 
					if (_liveChat == null)
					{
						_liveChat = Services.Get<LiveSupportChatService>();
					}

					if (_expertQuestions == null)
                    {
						_expertQuestions = Services.Get<ExpertQuestionService>();
                    }

					// Let's grab the current live chat.
					var chat = await _liveChat.Get(ctx, message.LiveSupportChatId);

					// Let's create a new expert question entry.
					var expertQuestion = await _expertQuestions.Create(ctx, new ExpertQuestion() { UserId = chat.UserId, FullName = chat.FullName, Email = chat.Email});

					// Let's also set the chat to have the expert Question id.
					chat.ExpertQuestionId = expertQuestion.Id;
					chat = await _liveChat.Update(ctx, chat);
				}
				*/

				// message.LiveSupportChatId
				List<ChatBotDecision> list = null;
				if(_inReplyToMap.TryGetValue(message.InReplyTo, out list) && list.Count > 0)
				{
					// Chatbot has something to say in response to this message.
					// Find the right response based on what the user sent. 
					ChatBotDecision noneResponse = null;
					ChatBotDecision dec = null;
					
					foreach(var entry in list)
					{
						var lsm = await Get(ctx, entry.Id);


						if(outOfHoursResponse)
                        {
							// We are looking for the out of hours response message type (in this project, type 15)
							if(entry.MessageType == 15)
                            {
								dec = lsm;
								break;
                            }
                        }
						else
                        {
							if (string.IsNullOrEmpty(lsm.AnswerProvided))
							{
								noneResponse = lsm;
							}
							else if (lsm.AnswerProvided == message.Message)
							{
								// User selected some previous response and sent it to the chatbot
								dec = lsm;
								break;
							}
						}
						
					}
					
					if(dec == null)
					{
						dec = noneResponse;
					}
					
					if(dec != null)
					{
						// Using this one.
						var lsm = await Get(ctx, dec.Id);

						await SendChatBotMessage(ctx, message.LiveSupportChatId, lsm);
					}
					
				}
				
				return message;
			});

			Events.LiveSupportChat.BeforeUpdate.AddEventListener(async (Context ctx, LiveSupportChat chat) =>
			{
				// If we are closing out a support chat by nullifying its EnteredQueue and AssignedToUserId
				// we need to send them a specified chatbot decision:
				// TODO: this needs to be made dynamic in future projects. 
				if (chat == null || ctx == null)
                {
					return null;
                }

				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}
				// Let's load the chat so we can see its current state.
				var currChat = await _liveChat.Get(ctx, chat.Id);

				// Now let's compare, if currChat has both assignedToUser set and enteredQueueUtc, and the new chat doesn't, we need to send message to the user to give control to the bot.
				if(chat.AssignedToUserId == null && chat.EnteredQueueUtc == null && currChat.AssignedToUserId != null && currChat.EnteredQueueUtc != null)
				{
					var alsoSendMessage = await Get(ctx, 11);
					await Task.Delay(1000);
					await SendChatBotMessage(ctx, chat.Id, alsoSendMessage);
				}

				return chat;

			});
			
			Events.ChatBotDecision.AfterCreate.AddEventListener((Context ctx, ChatBotDecision dec) => {
				AddToMap(dec);
				return new ValueTask<ChatBotDecision>(dec);
			});
			
			Events.ChatBotDecision.AfterUpdate.AddEventListener(async (Context ctx, ChatBotDecision dec) => {
				
				// Bulky - would be nicer to actually update only the entry that changed!
				await UpdateLookup();
				
				return dec;
			});
			
			Events.ChatBotDecision.AfterDelete.AddEventListener(async (Context ctx, ChatBotDecision dec) => {
				
				// Bulky - would be nicer to actually update only the entry that changed!
				await UpdateLookup();
				
				return dec;
			});
			
			Cache(new CacheConfig<ChatBotDecision>(){
				Preload = true,
				OnCacheLoaded = () => {

					// Called when the cache has everything in it.
					Task.Run(async () => {

						await UpdateLookup();

					});

				}
			});
		}
		
		private async Task SendChatBotMessage(Context ctx, int chatId, ChatBotDecision dec)
		{
			// Add an artificial delay to make sure sorts are ok:
			var time = DateTime.UtcNow.AddSeconds(1);
			var context = new Context()
			{
				UserId = 0
			};

			// Check the message text for key words such as {first}
			var returnText = dec.MessageText;

			var outOfHoursResponse = false;

			// Live Operator start
			if (dec.StartMode == 1)
            {
				// We need to check the hours currently.
				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}

				if (_liveChatHours == null)
				{
					_liveChatHours = Services.Get<LiveChatHourService>();
				}

				// Let's grab the live chat hours
				var hours = await _liveChatHours.Get(ctx, 1);

				// Is hours null? if so, let's let things carry on.
				if (hours != null)
				{
					// Let's grab the current time.
					var now = DateTime.UtcNow;
					var startHour = hours.AvailabilityStartUtc.Hour;
					var endHour = startHour + hours.Duration;
					if ((now.Hour >= startHour && now.Hour < endHour) || (now.Hour + 24 >= startHour && now.Hour + 24 < endHour))
					{
						// We are good!
						outOfHoursResponse = false;
					}
					else
					{
						// We are not good, we need to override the response.
						outOfHoursResponse = true;

						var ooh = await List(ctx, new Filter<ChatBotDecision>().Equals("MessageType", 15));

						if(ooh.Count > 0 )
                        {
							dec = ooh[0];
                        }
					}
				}
			}

			// Meeting start
			if (dec.StartMode == 2)
            {
				// We need to create a meeting Appointment instance - let's grab that service and the live chat service so we can get existing details. 
				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}

				if (_meetingsAppointments == null)
				{
					_meetingsAppointments = Services.Get<MeetingAppointmentService>();
				}

				// Let's grab the current live chat.
				var chat = await _liveChat.Get(ctx, chatId);

				// Let's create a new meeting appointment entry.
				var appt = await _meetingsAppointments.Create(ctx, new MeetingAppointment() { FullName = chat.FullName, Email = chat.Email, UserId = chat.UserId });

				// Let's also set the chat to have the appointment's id.
				chat.MeetingAppointmentId = appt.Id;
				chat = await _liveChat.Update(ctx, chat);
			}

			// expert question start.
			if (dec.StartMode == 3)
            {
				// We need to create a meeting Appointment instance - let's grab that service and the live chat service so we can get existing details. 
				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}

				if (_expertQuestions == null)
				{
					_expertQuestions = Services.Get<ExpertQuestionService>();
				}

				// Let's grab the current live chat.
				var chat = await _liveChat.Get(ctx, chatId);

				// Let's create a new expert question entry.
				var expertQuestion = await _expertQuestions.Create(ctx, new ExpertQuestion() { UserId = chat.UserId, FullName = chat.FullName, Email = chat.Email });

				// Let's also set the chat to have the expert Question id.
				chat.ExpertQuestionId = expertQuestion.Id;
				chat = await _liveChat.Update(ctx, chat);
			}



			if (dec.MessageText.Contains("{first}"))
            {
				var returnTextParts = dec.MessageText.Split("{first}");

				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}

				var chat = await _liveChat.Get(ctx, chatId);

				var first = chat.FullName.Split(" ")[0];

				returnText = returnTextParts[0] + first + returnTextParts[1];
			}

			if (dec.MessageText.Contains("{queue}"))
            {
				// Let's get the current queue count
				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}

				var chats = await _liveChat.List(ctx, new Filter<LiveSupportChat>().Not().Equals("EnteredQueueUtc", null).And().Equals("AssignedToUserId", null));

				var queuePosition = chats.Count + 1;

				returnText = returnText.Replace("{queue}", queuePosition.ToString());

			}

			if (dec.MessageText.Contains("{start"))
            {
				// Let's get the start time.
				if (_liveChatHours == null)
				{
					_liveChatHours = Services.Get<LiveChatHourService>();
				}

				// Let's grab the live chat hours
				var hours = await _liveChatHours.Get(ctx, 1);

				// Is hours null? if so, let's let things carry on.
				if (hours != null)
				{
					var startHour = hours.AvailabilityStartUtc.ToLocalTime().Hour;
					var startMinute = hours.AvailabilityStartUtc.ToLocalTime().Minute;

					var startMinuteStr = startMinute.ToString();
					
					if (startMinute < 10)
                    {
						startMinuteStr = "0" + startMinute;
                    }
					
					var startTime = startHour + ":" + startMinuteStr;
					returnText = returnText.Replace("{start}", startTime);
				}
			}

			if (dec.MessageText.Contains("{end}"))
            {
				// Let's get the end time.
				if (_liveChatHours == null)
				{
					_liveChatHours = Services.Get<LiveChatHourService>();
				}

				// Let's grab the live chat hours
				var hours = await _liveChatHours.Get(ctx, 1);

				// Is hours null? if so, let's let things carry on.
				if (hours != null)
				{
					var endHour = hours.AvailabilityStartUtc.ToLocalTime().Hour + hours.Duration;
					var endMinute = hours.AvailabilityStartUtc.ToLocalTime().Minute;

					var endMinuteStr = endMinute.ToString();

					if (endMinute < 10)
                    {
						endMinuteStr = "0" + endMinute;
                    }

					while (endHour > 24)
                    {
						endHour -= 24;
                    }

					var startTime = endHour + ":" + endMinuteStr;
					returnText = returnText.Replace("{end}", startTime);
				}
			}

			await _liveChatMessages.Create(context, new LiveSupportMessage(){
				LiveSupportChatId = chatId,
				Message = returnText,
				MessageType = dec.MessageType,
				FromSupport = true,
				ReplyTo = dec.ReplyToOverrideId.HasValue ? dec.ReplyToOverrideId.Value : dec.Id,
				PayloadJson = dec.PayloadJson,
				EditedUtc = time,
				CreatedUtc = time
			});

			// Excellent! Before resolving here, let's make sure this decision doesn't have an also send.
			if (dec.AlsoSend.HasValue)
            {
				var alsoSendMessage = await Get(ctx, dec.AlsoSend.Value);
				await Task.Delay(1000);
				await SendChatBotMessage(ctx, chatId, alsoSendMessage);
            }

			if (dec.MessageType == 2)
			{
				if (_liveChat == null)
				{
					_liveChat = Services.Get<LiveSupportChatService>();
				}

				var chat = await _liveChat.Get(ctx, chatId);
				chat.EnteredQueueUtc = DateTime.Now;
				chat = await _liveChat.Update(ctx, chat);
			}
		}
		
		private void AddToMap(ChatBotDecision dec)
		{
			if(_inReplyToMap.TryGetValue(dec.InReplyTo, out List<ChatBotDecision> list))
			{
				// Add it:
				list.Add(dec);
			}
			else
			{
				list = new List<ChatBotDecision>();
				_inReplyToMap[dec.InReplyTo] = list;
				list.Add(dec);
			}
		}
		
		private async Task UpdateLookup()
		{
			// Build a lookup for InReplyTo values, each containing the list of (optional) specific responses:
			// InReplyTo 0 represents the user opening a chat.
			var everything = await List(new Context(), new Filter<ChatBotDecision>());

			_inReplyToMap = new Dictionary<int, List<ChatBotDecision>>();

			foreach(var dec in everything)
			{
				AddToMap(dec);
			}
		}
	}
    
}
