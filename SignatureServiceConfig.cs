using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Signatures
{
	/// <summary>
	/// The appsettings.json config for the sig service. Usually used for prod/ stage.
	/// </summary>
    public class SignatureServiceConfig
    {
		/// <summary>
		/// Private key
		/// </summary>
		public string Private {get; set;}
		
		/// <summary>
		/// Public key
		/// </summary>
		public string Public {get; set;}
	}
	
}
