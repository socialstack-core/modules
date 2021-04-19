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

			if (!isUser && !Api.Database.ContentTypes.IsAssignableToGenericType(DefaultType, typeof(UserCreatedContent<>)))
			{
				return null;
			}

			return Add(new FilterFieldEqualsValue(DefaultType, isUser ? "Id" : "UserId", (Context ctx) => new ValueTask<uint>(ctx.UserId)));
		}

	}
}