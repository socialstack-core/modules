using System;
using Api.Contexts;
using Api.Users;


namespace Api.Permissions
{

	/// <summary>
	/// A filter method which is active if the current user (or their ID) is provided.
	/// </summary>
	public partial class FilterIfSelf : FilterFieldEquals
	{
		/// <summary>
		/// Create a new ifSelf node.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="field"></param>
		public FilterIfSelf(Type type, string field) : base(type, field) { }

		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override bool IsGranted(Capability capability, Context token, object[] extraObjectsToCheck)
		{
			// Get first extra arg
			if (extraObjectsToCheck == null || extraObjectsToCheck.Length < ArgIndex)
			{
				// Arg not provided. Hard fail scenario.
				return EqualsFail(capability);
			}

			var firstArg = extraObjectsToCheck[ArgIndex];

			// Firstly is it a direct match?
			if (firstArg == null)
			{
				return token == null;
			}

			// ID to compare to:
			var currentUserId = token == null ? 0 : token.UserId;

			if (firstArg.Equals(currentUserId))
			{
				return true;
			}

			// Nope - try matching it via reading the field next.
			if (firstArg.GetType() != Type)
			{
				return false;
			}

			return currentUserId.Equals(FieldInfo.GetValue(firstArg));
		}

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterIfSelf(Type, Field);
		}
	}

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
			return Add(new FilterIfSelf(type, field));
		}

	}

}