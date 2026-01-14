using Api.Blogs;
using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using System.Threading.Tasks;


/// <summary>
/// Handles installing new role(s) for blogs. Occurs in an event listener such that it 
/// can catch it when the role service starts up rather than deferred to when the blog service does.
/// </summary>
[EventListener]
public class BlogEventListener
{
	/// <summary>
	/// </summary>
	public BlogEventListener()
	{
		Events.Role.CollectCustomRoles.AddEventListener((Context ctx, RoleService roleService) => {
			roleService.Install(new Role() {
				Key = "blogger",
				Name = "Blogger",
				CanViewAdmin = true
			});
			
			return new ValueTask<RoleService>(roleService);
		});

		Events.Role.Register.AddEventListener((Context context, Role role) => {

			if (role == null)
			{
				return new ValueTask<Role>(role);
			}

			if (role.Key == "blogger")
			{
				Roles.Blogger = role;
			}

			return new ValueTask<Role>(role);
		});

	}
}