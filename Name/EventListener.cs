using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Newtonsoft.Json;
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

			ComposableChangeField fullName = null;

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
