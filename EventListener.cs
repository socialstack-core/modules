using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Api.Users
{
    /// <summary>
    /// Sets fullname
    /// </summary>
	[EventListener]
    public class NameEventHandler
    {
       /// <summary>
	   /// Sets fullname
	   /// </summary>
	   public NameEventHandler()
	   {

			ComposableChangeField fullName = null;
			ComposableChangeField firstLast = null;
			
			Events.User.BeforeSettable.AddEventListener((Context ctx, JsonField<User, uint> field) => {

				if (field == null)
				{
					return new ValueTask<JsonField<User, uint>>(field);
				}

				if (field.Name == "FirstName")
				{
					// When FirstName is set, update FullName automatically.
					field.OnSetValue.AddEventListener((Context ctx, object v, User user, Newtonsoft.Json.Linq.JToken token) =>
					{
						if (fullName == null)
						{
							fullName = Services.Get<UserService>().GetChangeField("FullName");
						}

						var fName = token.Type == JTokenType.String ? (token as JToken).Value<string>() : null;

						if (string.IsNullOrEmpty(fName))
						{
							if (string.IsNullOrEmpty(user.LastName))
							{
								user.FullName = "";
							}
							else
							{
								user.FullName = user.LastName;
							}
						}
						else if (string.IsNullOrEmpty(user.LastName))
						{
							user.FullName = fName;
						}
						else
						{
							user.FullName = fName + " " + user.LastName;
						}

						user.MarkChanged(fullName);

						return new ValueTask<object>(v);
					});
				}
				else if (field.Name == "LastName")
				{
					// When LastName is set, update FullName automatically.
					field.OnSetValue.AddEventListener((Context ctx, object v, User user, Newtonsoft.Json.Linq.JToken token) =>
					{
						if (fullName == null)
						{
							fullName = Services.Get<UserService>().GetChangeField("FullName");
						}

						var lName = token.Type == JTokenType.String ? (token as JToken).Value<string>() : null;

						if (string.IsNullOrEmpty(user.FirstName))
						{
							if (string.IsNullOrEmpty(lName))
							{
								user.FullName = "";
							}
							else
							{
								user.FullName = lName;
							}
						}
						else if (string.IsNullOrEmpty(lName))
						{
							user.FullName = user.FirstName;
						}
						else
						{
							user.FullName = user.FirstName + " " + lName;
						}

						user.MarkChanged(fullName);

						return new ValueTask<object>(v);
					});
				}
				else if (field.Name == "FullName")
				{
					// When FullName is set, update First + LastName automatically.
					field.OnSetValue.AddEventListener((Context ctx, object v, User user, Newtonsoft.Json.Linq.JToken token) =>
					{
						if (firstLast == null)
						{
							firstLast = Services.Get<UserService>().GetChangeField("FirstName").And("LastName");
						}

						var fullName = token.Type == JTokenType.String ? (token as JToken).Value<string>() : null;

						if (fullName == null)
						{
							user.FirstName = null;
							user.LastName = null;
						}
						else
						{
							fullName = fullName.Trim();
							var firstSpace = fullName.IndexOf(' ');
							if (firstSpace == -1)
							{
								user.FirstName = fullName;
								user.LastName = null;
							}
							else
							{
								user.FirstName = fullName.Substring(0, firstSpace);
								user.LastName = fullName.Substring(firstSpace + 1);
							}
						}

						user.MarkChanged(firstLast);

						return new ValueTask<object>(v);
					});
				}

				return new ValueTask<JsonField<User, uint>>(field);
			});

			// For users created via other APIs:
		   Events.User.BeforeCreate.AddEventListener((Context ctx, User user) => {
			   
			   if(user == null)
			   {
				   return new ValueTask<User>(user);
			   }
			   
			   if(string.IsNullOrEmpty(user.FirstName))
			   {
				   if(string.IsNullOrEmpty(user.LastName))
				   {
					   user.FullName = "";
				   }
				   else
				   {
					   user.FullName = user.LastName;
				   }
			   }
			   else if(string.IsNullOrEmpty(user.LastName))
			   {
				   user.FullName = user.FirstName;
			   }
			   else
			   {
				   user.FullName = user.FirstName + " " + user.LastName;
			   }
			   
			   return new ValueTask<User>(user);
		   });

		   Events.User.BeforeUpdate.AddEventListener((Context ctx, User user) => {
			   
			   if(user == null)
			   {
				   return new ValueTask<User>(user);
			   }
			   
			   string newFullName;
			   
			   if(string.IsNullOrEmpty(user.FirstName))
			   {
				   if(string.IsNullOrEmpty(user.LastName))
				   {
					   newFullName = "";
				   }
				   else
				   {
					   newFullName = user.LastName;
				   }
			   }
			   else if(string.IsNullOrEmpty(user.LastName))
			   {
				   newFullName = user.FirstName;
			   }
			   else
			   {
				   newFullName = user.FirstName + " " + user.LastName;
			   }
			   
			   if(newFullName != user.FullName)
			   {
				   if (fullName == null)
				   {
					   fullName = Services.Get<UserService>().GetChangeField("FullName");
				   }

				   user.FullName = newFullName;
				   user.MarkChanged(fullName);
			   }
			   
			   return new ValueTask<User>(user);
		   });
		   
	   }
	   
    }
    
}
