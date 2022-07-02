using System;
using System.Collections.Generic;
using System.Linq;
using Api.Configuration;
using System.Threading.Tasks;


namespace Api.TwoFactorGoogleAuth
{
	/// <summary>
	/// Two factor auth cfg
	/// </summary>
    public partial class TwoFactorAuthConfig : Config
    {
		/// <summary>
		/// Set true if 2fa is required on all accounts.
		/// </summary>
		public bool Required {get; set;} = false;
		
		/// <summary>
		/// True if its required for any admin accessing accounts (any role with CanViewAdmin true).
		/// </summary>
		public bool RequiredForAdmin {get; set;} = true;
    }
	
}
