using Amazon;
using Api.Configuration;
using CryptSharp.Internal;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using SharpCompress.Common;
using Stripe.Tax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>
	/// Configuration for Opayo
	/// 
	/// The basic profile account has the AVS / CV2 and 3D Secure checks turned off by default. This account is used in most of our code examples.
	/// Basic profile:
	/// vendorName: sandbox
	/// integrationKey: hJYxsw7HLbj40cB8udES8CDRFLhuJ8G54O6rDpUXvE6hYDrria
	///	integrationPassword: o2iHSrFybYMZpmWOQMuhsXP52V4fBtpuSDshrKDSWsBY1OiN6hwd9Kb12z4j5Us5u
	/// The extra checks profile account has strict AVS / CV2 checks and 3D Secure authentication enabled.Any transaction that does not pass either the AVS or CV2 checks will be rejected.The 3D secure checks are turned on and successful transaction registration will result in a request for 3D Secure authentication.
	/// Extra checks profile:
	/// vendorName: sandboxEC
	/// integrationKey: dq9w6WkkdD2y8k3t4olqu8H6a0vtt3IY7VEsGhAtacbCZ2b5Ud
	/// integrationPassword: hno3JTEwDHy7hJckU4WuxfeTrjD0N92pIaituQBw5Mtj7RG3V8zOd
	/// 
	/// In order for a transaction to pass the AVS checks you will need to provide the following details in the billingAddress object:
	/// "address1": "88"
	/// "postalCode": "412"
	/// In order for a transaction to pass the CV2 checks you will have to enter the following security code when creating a card identifier:
	/// "securityCode": "123"
	/// You can always override the default security settings by using the applyAvsCvcCheck and apply3DSecure properties when posting to the transactions endpoint.
	/// </summary>
	public class OpayoConfig : Config
	{
		/// <summary>
		/// Your Opayo integration Key
		/// </summary>
		[Frontend]
		public string IntegrationKey { get; set; } = "";

		/// <summary>
		/// Your Opayo integration passwword
		/// </summary>
		public string IntegrationPassword { get; set; } = "";

		/// <summary>
		/// The Vendor Name assigned to you by Opayo
		/// </summary>
		public string VendorName { get; set; } = "";

		/// <summary>
		/// The Opayo reat API host used by opayo.js
		/// sandbox - https://sandbox.opayo.eu.elavon.com
		/// live    - https://live.opayo.eu.elavon.com
		/// </summary>
		[Frontend]
		public string RestHost { get; set; } = "https://sandbox.opayo.eu.elavon.com";

		/// <summary>
		/// The Opayo rest base url used by opayo.js 
		/// sandbox - https://sandbox.opayo.eu.elavon.com/api/v1/
		/// live    - https://live.opayo.eu.elavon.com/api/v1/
		/// </summary>
		[Frontend]
		public string RestBaseURL { get; set; } = "https://sandbox.opayo.eu.elavon.com/api/v1/";

		/// <summary>
		/// Use this field to override your default account level 3D Secure settings
		/// UseMSPSetting - Use default MyOpayo settings.
		/// Force         - Apply authentication even if turned off.
		/// Disable       - Only use this if you intend to use an SCA Exemption. See here for more information.).
		/// </summary>
		public string Apply3DSecure { get; set; } = "UseMSPSetting";

		/// <summary>
		/// Use this field to override your default account level AVS CVC settings.
		/// UseMSPSetting      - Use default MyOpayo settings.
		/// Force              - Apply authentication even if turned off.
		/// Disable            - Disable authentication and rules.
		/// ForceIgnoringRules - Apply authentication but ignore rules.
		/// </summary>
		public string ApplyAvsCvcCheck { get; set; } = "UseMSPSetting";

		/// <summary>
		/// Expose additional logging to trace payments when testing
		/// </summary>
		public bool VerboseLogging {get;set;} = false;

		/// <summary>
		/// The call back endpoint (used to override value from app settings)
		/// </summary>
		public string CallbackUrl { get; set; }

	}
}
