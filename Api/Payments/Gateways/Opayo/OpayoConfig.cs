using Api.Configuration;

namespace Api.Payments
{
	/// <summary>
	/// Configuration for Opayo
	/// 
	/// The basic profile account has the AVS / CV2 and 3D Secure checks turned off by default. This account is used in most of our code examples.
	/// Basic profile:
	/// 
	/// vendorName: sandbox
	/// integrationKey: hJYxsw7HLbj40cB8udES8CDRFLhuJ8G54O6rDpUXvE6hYDrria
	///	integrationPassword: o2iHSrFybYMZpmWOQMuhsXP52V4fBtpuSDshrKDSWsBY1OiN6hwd9Kb12z4j5Us5u
	/// The extra checks profile account has strict AVS / CV2 checks and 3D Secure authentication enabled.Any transaction that does not pass either the AVS or CV2 checks will be rejected.The 3D secure checks are turned on and successful transaction registration will result in a request for 3D Secure authentication.
	/// Extra checks profile:
	/// 
	/// vendorName: sandboxEC
	/// integrationKey: dq9w6WkkdD2y8k3t4olqu8H6a0vtt3IY7VEsGhAtacbCZ2b5Ud
	/// integrationPassword: hno3JTEwDHy7hJckU4WuxfeTrjD0N92pIaituQBw5Mtj7RG3V8zOd
	/// 
	/// In order for a transaction to pass the AVS checks you will need to provide the following details in the billingAddress object:
	/// "address1": "88"
	/// "postalCode": "412"
	/// In order for a transaction to pass the CV2 checks you will have to enter the following security code when creating a card identifier:
	/// "securityCode": "123"
	/// You can always override the default security settings by using the applyAvsCvcCheck and apply3DSecure properties when posting to the transactions endpoint. /// 
	/// 
	/// </summary>
	public class OpayoConfig : Config
	{
		/// <summary>
		/// Your Opayo integration Key
		/// </summary>
		public string IntegrationKey { get; set; } = "";

		/// <summary>
		/// Your Opayo shortcode used to easily identify payment type and pass to 3rd paries 
		/// </summary>
		public string ShortCode { get; set; } = "SAG";

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
		/// Indicates the reason you're bypassing 3D Secure authentication. Use in conjunction with Apply3DSecure:Disable
		///  You must first get permission from your acquirer to use exemptions. Using exemptions means that fraud liability, for the transaction, is shifted to you the merchant. 
		///  If the card issuer does not agree with the exemption, they can return a 'soft decline' - response code of bankResponseCode:65 or bankResponseCode:1A - Opayo will automatically request for 3D Secure authentication and submit another authorisation request to the card issuer with 3D Secure authentication data. 
		///  It is important that you correctly populate the required field values for the strongCustomerAuthentication object.
		///  
		/// 
		/// </summary>
		public string ThreeDSExemptionIndicator { get; set; } = "";

		/// <summary>
		/// Use this field to override your default account level AVS CVC settings.
		/// UseMSPSetting      - Use default MyOpayo settings.
		/// Force              - Apply authentication even if turned off.
		/// Disable            - Disable authentication and rules.
		/// ForceIgnoringRules - Apply authentication but ignore rules.
		/// </summary>
		public string ApplyAvsCvcCheck { get; set; } = "UseMSPSetting";

		/// <summary>
		/// Transaction type indicator for 3DS.
		/// 
		/// GoodsAndServicePurchase = Goods/ Service Purchase
		/// CheckAcceptance = Check Acceptance
		/// AccountFunding = Account Funding
		/// QuasiCashTransaction = Quasi-Cash Transaction
		/// PrepaidActivationAndLoad = Prepaid Activation and Load
		/// Values derived from the 8583 ISO Standard.     
		/// </summary>
		public string TransType { get; set; } = "GoodsAndServicePurchase";

		/// <summary>
		/// Expose additional logging to trace payments when testing
		/// </summary>
		public bool VerboseLogging {get;set;} = false;

		/// <summary>
		/// Flag to expose if opayo is enabled 
		/// </summary>
		[Frontend]
		public bool IsEnabled { get; set; } = false;

		/// <summary>
		/// Flag to indicate if we use our own hosted form or the merchants iframe 
		/// This is related to the level of PCI complianace
		/// </summary>
		[Frontend]
		public bool OwnFormEnabled { get; set; } = false;

		/// <summary>
		/// Flag to indicate if we use of merchants hosted page
		/// This is related to the level of PCI complianace
		/// </summary>
		[Frontend]
		public bool HostedPageEnabled { get; set; } = true;

		/// <summary>
		/// Flag to expose if opayo is able to save card details for reuse
		/// This requires 3ds to be enabled and configured 
		/// </summary>
		[Frontend]
		public bool CanSaveCards { get; set; } = false;

	}
}
