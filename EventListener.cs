using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Contexts;
using System.Threading;
using Api.WebSockets;
using Api.Eventing;
using System.Net.Http;
using Api.Users;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Web;
using System.IO;

namespace Api.SilverbearAuth
{

	/// <summary>
	/// Hooks up the auth method to login using silverbear DNN auth details.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		private HttpClient _client = new HttpClient();
		private IUserService _users;
		private string _headerUsername;
		private string _headerPassword;
		private string _serviceUrl;
		
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Get the cfg:
			var auth = AppSettings.GetSection("silverbearAuth");
			
			_headerUsername = auth["HeaderUsername"];
			_headerPassword = auth["HeaderPassword"];
			_serviceUrl = auth["ServiceUrl"];
			
			Events.UserOnAuthenticate.AddEventListener(async (Context context, LoginResult result, UserLogin body) => {
					
				if (result != null)
				{
					// Some other auth handler has already authed this user.
					return result;
				}

				if (_users == null)
				{
					_users = Services.Get<IUserService>();
				}

				var username = body.Email;

				// Hit DNN for the auth token:
				var args = new Dictionary<string, string>();
				args["Username"] = username;
				args["Password"] = body.Password;
					
				// Get the auth token:
				var xmlEle = await DnnRequest("GetAuthenticationToken", args);

				// If it's not valid, failed.
				if (xmlEle == null)
				{
					return null;
				}

				var authToken = "";
					
				// Get the (local) user info by username:
				var user = await _users.GetByUsername(context, username);

				if (user == null)
				{
					// Next, get the user details:
					args = new Dictionary<string, string>();
					args["AuthenticationToken"] = authToken;
					xmlEle = await DnnRequest("GetUserDetails", args);

					// Create it:
					user = await _users.Create(context, new User()
					{
						Username = "",
						FirstName = "",
						LastName = "",
						// Password token is intentionally null.
						Email = ""
					});
				}
				return new LoginResult() {
					User = user
				};
					
			}, 20);
		}
		
		/// <summary>
		/// Hits the DNN API.
		/// </summary>
		public async Task<XElement> DnnRequest(string endpoint, Dictionary<string, string> args){
			
			args["HeaderUsername"] = _headerUsername;
			args["HeaderPassword"] = _headerPassword;
			args["HeaderPortalId"] = "0";
			
			var url = _serviceUrl + "DesktopModules/WebServices/CoreService.svc/IWebSingleSignOnRestManager/" + endpoint + "?";
			bool first=true;
			
			foreach(var kvp in args){
				if(first){
					first=false;
				}else{
					url+="&";
				}
				
				url += kvp.Key + "=" + HttpUtility.UrlEncode(kvp.Value);
			}

			try
			{
				var response = await _client.GetAsync(url);
				StringReader reader = new StringReader(await response.Content.ReadAsStringAsync());
				XDocument document = XDocument.Load(reader);
				return document.Root;
			}
			catch(Exception e)
			{
				Console.WriteLine(e.ToString());

				// Not XML/ http request failed etc.
				return null;
			}
		}
		
	}
}
