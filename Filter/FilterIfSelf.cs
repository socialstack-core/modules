using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Users;


namespace Api.Permissions
{
	public partial class Filter
	{

		/// <summary>
		/// True if an ID or user object given equals that of the current user.
		/// </summary>
		/// <returns></returns>
		public Filter IsSelf()
		{
			// Types that aren't UserCreatedRow or User itself return false always.
			var isUser = DefaultType == typeof(User);

			if (!isUser && !typeof(UserCreatedRow).IsAssignableFrom(DefaultType))
			{
				return null;
			}

			return Add(new FilterFieldEqualsValue(DefaultType, isUser ? "Id" : "UserId", (Context ctx) => Task.FromResult((object)ctx.UserId)));
		}

	}
}