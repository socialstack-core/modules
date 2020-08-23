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
		/// Short for IsSelf(typeof(User), "Id")
		/// </summary>
		/// <returns></returns>
		public Filter IsSelf()
		{
			return IsSelf(typeof(User), "Id");
		}

		/// <summary>
		/// True if an ID or user object given equals that of the current user.
		/// Short for IsSelf(typeof(User), "Id")
		/// </summary>
		/// <returns></returns>
		public Filter IsSelf(Type type, string field)
		{
			return Add(new FilterFieldEqualsValue(type, field, (Context ctx) => Task.FromResult((object)ctx.UserId)));
		}

	}
}