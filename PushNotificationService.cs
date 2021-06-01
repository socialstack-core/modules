using System;
using System.Text;
using Api.Contexts;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using System.Net.Http;
using Api.Configuration;
using System.Timers;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net.Security;

namespace Api.PushNotifications
{
    /// <summary>
    /// Handles sending of push notifications.
    /// Intanced automatically. Use Injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class PushNotificationService : AutoService
    {
		private HttpClient _firebaseClient = new HttpClient();
        private PushNotificationConfig _configuration;
		private long MessageId = 1;

		private StringBuilder BatchQueue = new StringBuilder();
		private SslStream Connection;
		private byte[] ReadBuffer = new byte[4000];

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PushNotificationService()
		{
			_configuration = AppSettings.GetSection("PushNotifications").Get<PushNotificationConfig>();

			_firebaseClient = new HttpClient();
			_firebaseClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "key=" + _configuration.ServerKey);

			var sendTimer = new Timer();
			sendTimer.Interval = 1000;
			sendTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
			{
				if (BatchQueue.Length > 0)
				{
					SendNow();
				}
			};
			sendTimer.Enabled = true;
			sendTimer.Start();
		}

		private void SendXML(string xml)
		{
			if (xml == null)
			{
				return;
			}

			try
			{
				Connection.Write(System.Text.Encoding.UTF8.GetBytes(xml));
			}
			catch
			{
				// Socket issue - drop the connection and try again later.
				Console.WriteLine("Warning: Push notification link dropped. It will be recreated, but some notifications will have also been dropped too.");
				Connection = null;
				ConnectState = 0;
			}
		}

		private void ReadResponse()
		{
			Connection.BeginRead(ReadBuffer, 0, ReadBuffer.Length, OnReadResponse, this);
		}

		private void OnReadResponse(IAsyncResult result)
		{
			var bytesRead = 0;

			try
			{
				bytesRead = Connection.EndRead(result);

				if (bytesRead != 0)
				{
					string response = null;
					if (ConnectState != 4)
					{
						response = System.Text.Encoding.UTF8.GetString(ReadBuffer, 0, bytesRead);
					}
					
					switch (ConnectState)
					{
						case 0:
							if (response.Contains("stream:features"))
							{
								// Authenticate now:
								var plain = Base64Encode("\x00" + _configuration.SenderId + "@fcm.googleapis.com\x00" + _configuration.ServerKey);
								ConnectState = 1;
								SendXML("<auth mechanism='PLAIN' xmlns='urn:ietf:params:xml:ns:xmpp-sasl'>" + plain + "</auth>");
							}

						break;
						case 1:
							if (response.Contains("success"))
							{
								SendXML("<stream:stream to='fcm.googleapis.com' version='1.0' xmlns='jabber:client' xmlns:stream='http://etherx.jabber.org/streams'>");
								ConnectState = 2;
							}
							else
							{
								Console.WriteLine("Firebase push notification issue:");
								Console.WriteLine(response);
							}

						break;
						case 2:
							if (response.Contains("stream:features"))
							{
								// Bind now:
								ConnectState = 3;
								SendXML("<iq type='set'><bind xmlns='urn:ietf:params:xml:ns:xmpp-bind'></bind></iq>");
							}
						break;
						case 3:
							// Binded and ready to go.
							ConnectState = 4;
							var oc = OnConnected;
							OnConnected = null;

							try
							{
								if (oc != null)
								{
									foreach (var action in oc)
									{
										action();
									}
								}
							}
							catch (Exception e)
							{
								Console.WriteLine("Warning: Push notification onConnect handle failed " + e.ToString());
							}
						break;
					}
					
				}
			}
			catch
			{
				Console.WriteLine("Warning: Push notification link dropped. It will be recreated, but some notifications will have also been dropped too.");
				Connection = null;
				ConnectState = 0;
			}

			if (bytesRead == 0)
			{
				return;
			}

			ReadResponse();
		}

		private int ConnectState = 0;
		private List<Action> OnConnected;

		private void Connect(Action onConnect)
		{
			OnConnected = null;
			var host = "fcm-xmpp.googleapis.com";
			var client = new TcpClient(host, 5235);
			Connection = new SslStream(client.GetStream());
			Connection.AuthenticateAsClient(host);
			ReadResponse();
			
			// Send requests:
			SendXML("<stream:stream to='fcm.googleapis.com' version='1.0' xmlns='jabber:client' xmlns:stream='http://etherx.jabber.org/streams'>");
			ConnectState = 0;

			if (onConnect != null)
			{
				OnConnected = new List<Action>();
			}

			OnConnected.Add(onConnect);
		}

		private static string Base64Encode(string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return System.Convert.ToBase64String(plainTextBytes);
		}

		private void SendNow()
		{
			if (BatchQueue.Length == 0)
			{
				return;
			}

			string payload = null;

			lock (BatchQueue)
			{
				payload = BatchQueue.ToString();
				BatchQueue.Length = 0;
			}

			if (Connection == null)
			{
				Connect(() =>
				{
					SendXML(payload);
				});
			}
			else if (ConnectState != 4)
			{
				if (OnConnected == null)
				{
					OnConnected = new List<Action>();
				}

				OnConnected.Add(() =>
				{
					SendXML(payload);
				});
			}
			else
			{
				SendXML(payload);
			}
			
		}

		/// <summary>
		/// Sends a push notification.
		/// </summary>
		/// <returns>
		/// Returns true if the notification was added to the queue.
		/// </returns>
		public bool Send(Context context, PushNotification notification)
		{
			/*
			if (notification.TargetDeviceType == "apns")
			{
				// APNS (iOS)
				using (var apn = new ApnSender(_configuration.APNSKey, _configuration.APNSKeyId, _configuration.APNSTeamId, _configuration.iOSBundleId))
				{
					await apn.SendAsync("{\"aps\":{\"alert\":\"Test!\"}}", notification.TargetDevice);
				}
			}
			else
			{
			*/
			
			PushNotificationCustomData customData = notification.CustomData;

			if (customData == null)
			{
				customData = new PushNotificationCustomData()
				{
					body = notification.Body,
					title = notification.Title,
					url = notification.Url
				};
			}
			else
			{
				customData.body = notification.Body;
				customData.title = notification.Title;
				customData.url = notification.Url;
			}

			var message_id = "" + (++MessageId);

			// Firebase for everything else
			var payload = new
			{
				to = notification.TargetDevice,
				notification = new {
					body = notification.Body,
					title = notification.Title,
					url = notification.Url,

					//customData.category,
					//customData.click_action
				},
				data = customData,
				message_id,
			};
			
			var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

			lock (BatchQueue)
			{
				BatchQueue.Append("<message id='");
				BatchQueue.Append(message_id);
				BatchQueue.Append("'><gcm xmlns='google:mobile:data'>");
				BatchQueue.Append(serialized);
				BatchQueue.Append("</gcm></message>");
			}

			return true;
		}
		
	}
}