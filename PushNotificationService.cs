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
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Api.PushNotifications
{
    /// <summary>
    /// Handles sending of push notifications.
    /// Intanced automatically. Use Injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class PushNotificationService : IPushNotificationService
    {
		private HttpClient _firebaseClient = new HttpClient();
        private PushNotificationConfig _configuration;
		
        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public PushNotificationService()
        {
			_configuration = AppSettings.GetSection("PushNotifications").Get<PushNotificationConfig>();
			
			_firebaseClient = new HttpClient();
			_firebaseClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization","key="+_configuration.ServerKey);
        }
		
        /// <summary>
        /// Sends a push notification.
        /// </summary>
		/// <returns>
		/// If successful, the push notification is returned with a successful state.
		/// </returns>
		public async Task<PushNotification> Send(Context context, PushNotification notification)
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
					title = notification.Title
				};
			}
			else
			{
				customData.body = notification.Body;
				customData.title = notification.Title;
			}
			
			// Firebase for everything else
			var payload = new
			{
				to = notification.TargetDevice,
				notification = new {
					body = notification.Body,
					title = notification.Title,
					//customData.category,
					//customData.click_action
				},
				data = customData
			};
			
			var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

			try
			{
				var response = await _firebaseClient.PostAsync("https://fcm.googleapis.com/fcm/send", content);
				StringReader reader = new StringReader(await response.Content.ReadAsStringAsync());

				var resultJson = reader.ReadToEnd();

				var resultObject = Newtonsoft.Json.JsonConvert.DeserializeObject(resultJson);

				// TODO: if result indicates success, then:
				notification.Successful = true;

			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());

				// Not JSON/ http request failed etc.
				return null;
			}
			
			return notification;
		}
		
	}
}