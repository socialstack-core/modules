using Api.Rewards;
using System.Collections.Generic;

namespace Api.Users 
{
    public partial class User
    {
		/// <summary>
		/// This user's rewards.
		/// </summary>
        public List<Reward> Rewards { get; set;}
    }

	public partial class UserProfile
	{
		/// <summary>
		/// This user's rewards.
		/// </summary>
		public List<Reward> Rewards;
	}
}