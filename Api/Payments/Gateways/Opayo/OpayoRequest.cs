using Api.Addresses;
using Api.Payments.Opayo.Response;
using Newtonsoft.Json;

namespace Api.Payments.Opayo.Request
{

	/// <summary>
	/// Represents an Opayo transaction request payload with customer, billing, shipping, and SCA details.
	/// </summary>
	public class TransactionRequest
	{
		/// <summary>
		/// Type of transaction (e.g., Payment Deferred Authenticate Refund RepeatAuthorise).
		/// </summary>
		[JsonProperty("transactionType")] 
		public string TransactionType { get; set; }

		/// <summary>
		/// Your Opayo vendor account name.
		/// </summary>
		[JsonProperty("vendorName")] 
		public string VendorName { get; set; }

		/// <summary>
		/// Payment method details such as card information.
		/// </summary>
		[JsonProperty("paymentMethod")] 
		public PaymentMethodContainer PaymentMethod { get; set; }

		/// <summary>
		/// Unique code for this transaction in your system.
		/// </summary>
		[JsonProperty("vendorTxCode")] 
		public string VendorTxCode { get; set; }

		/// <summary>
		/// Amount to be charged in minor units (e.g., pennies).
		/// </summary>
		[JsonProperty("amount")] 
		public int Amount { get; set; }

		/// <summary>
		/// ISO 4217 currency code (e.g., GBP, USD).
		/// </summary>
		[JsonProperty("currency")] 
		public string Currency { get; set; }

		/// <summary>
		/// Free-text description of the transaction.
		/// </summary>
		[JsonProperty("description")] 
		public string Description { get; set; }

		/// <summary>
		/// Optional settlement reference text shown on settlement reports.
		/// </summary>
		[JsonProperty("settlementReferenceText")] 
		public string SettlementReferenceText { get; set; }

		/// <summary>
		/// Customer's first name.
		/// </summary>
		[JsonProperty("customerFirstName")] 
		public string CustomerFirstName { get; set; }

		/// <summary>
		/// Customer's last name.
		/// </summary>
		[JsonProperty("customerLastName")] 
		public string CustomerLastName { get; set; }

		/// <summary>
		/// Billing address information.
		/// </summary>
		[JsonProperty("billingAddress")] 
		public BillingAddress BillingAddress { get; set; }

		/// <summary>
		/// Entry method (e.g., Ecommerce MailOrder TelephoneOrder ContinuousAuthority).
		/// </summary>
		[JsonProperty("entryMethod")] 
		public string EntryMethod { get; set; }

		/// <summary>
		/// Indicates whether Gift Aid applies.
		/// </summary>
		[JsonProperty("giftAid")] 
		public bool GiftAid { get; set; }

		/// <summary>
		/// 3-D Secure configuration 
		/// Use this field to override your default account level 3D Secure settings
		/// UseMSPSetting - Use default MyOpayo settings.
		/// Force - Apply authentication even if turned off.
		/// Disable - Only use this if you intend to use an SCA Exemption. See here for more information.).
		/// </summary>
		[JsonProperty("apply3DSecure")] 
		public string Apply3DSecure { get; set; }

		/// <summary>
		/// AVS/CV2 check configuration
		/// Use this field to override your default account level AVS CVC settings.
		/// UseMSPSetting - Use default MyOpayo settings.
		/// Force - Apply authentication even if turned off.
		/// Disable - Disable authentication and rules.
		/// ForceIgnoringRules - Apply authentication but ignore rules.
		/// </summary>
		[JsonProperty("applyAvsCvcCheck")] 
		public string ApplyAvsCvcCheck { get; set; }

		/// <summary>
		/// Customer email address.
		/// </summary>
		[JsonProperty("customerEmail")] 
		public string CustomerEmail { get; set; }

		/// <summary>
		/// Customer's home phone number 
		/// This could also be the same as their mobile phone number if they do not have a home phone. 
		/// The customerPhone must be in the format of + and country code and phone number. 
		/// Example: For a UK phone number of 03069 990210, you will submit the following: +443069990210. 
		/// This must be provided for 3D Secure checking and fraud screening purposes.
		/// </summary>
		[JsonProperty("customerPhone")] 
		public string CustomerPhone { get; set; }

		/// <summary>
		/// Shipping details for physical goods delivery.
		/// </summary>
		[JsonProperty("shippingDetails")] 
		public ShippingDetails ShippingDetails { get; set; }

		/// <summary>
		/// Optional referrer identifier for tracking.
		/// This can be used to send the unique reference for the partner that referred the merchant to Opayo.
		/// </summary>
		[JsonProperty("referrerId")] 
		public string ReferrerId { get; set; }

		/// <summary>
		/// Strong Customer Authentication context and browser parameters.
		/// </summary>
		[JsonProperty("strongCustomerAuthentication")] 
		public StrongCustomerAuthentication StrongCustomerAuthentication { get; set; }

		/// <summary>
		/// Customer mobile phone number.
		/// The customerMobilePhone must be in the format of + and country code and phone number. 
		/// Example: For a UK phone number of 07234 567891, you will submit the following: +447234567891 
		/// When using the strongCustomerAuthentication object, it is advisable to provide this data if the cardholder has it. 
		/// This will assist the card issuer when determining the 3D Secure authentication result.		 
		/// </summary>
		[JsonProperty("customerMobilePhone")] 
		public string CustomerMobilePhone { get; set; }

		/// <summary>
		/// Customer work phone number.
		/// </summary>
		[JsonProperty("customerWorkPhone")] 
		public string CustomerWorkPhone { get; set; }
        
		/// <summary>
		/// Credential-on-file type details for MIT/COF.
		///The credentialType object is required when creating a token Save:true or using a token reusable:true or for Repeat transactions where transactionType:Repeat. 
		/// It advises the card issuer the reason why your storing a credential on file, and when using a stored credential.		/// 
		/// </summary>
		[JsonProperty("credentialType")] 
		public CredentialType CredentialType { get; set; }

		/// <summary>
		/// The fiRecipient object is required if your Merchant Category Code (MCC) has a value of 6012 (Financial Institution). 
		/// This is a card scheme mandated requirement. 
		/// If provided in the transaction request, the fiRecipient object and it's data will be returned in the transaction response.
		/// </summary>
		[JsonProperty("fiRecipient")]
		public FiRecipient FiRecipient { get; set; }

	}

	/// <summary>
	/// Specifies the payment method for the transaction.
	/// </summary>
	public class PaymentMethodContainer
	{
		/// <summary>
		/// The credit or debit card details for this transaction.
		/// </summary>
		[JsonProperty("card")]
		public Card Card { get; set; }
	}

	/// <summary>
	/// Card tokenization and storage preferences for Opayo transactions.
	/// </summary>
	public class Card
	{
		/// <summary>
		/// Merchant session key used for client-side tokenization.
		/// </summary>
		[JsonProperty("merchantSessionKey")]
		public string MerchantSessionKey { get; set; }

		/// <summary>
		/// Card identifier token returned by Opayo.
		/// </summary>
		[JsonProperty("cardIdentifier")]
		public string CardIdentifier { get; set; }

		/// <summary>
		/// Indicates whether the token can be reused.
		/// </summary>
		[JsonProperty("reusable")]
		public bool Reusable { get; set; }

		/// <summary>
		/// Indicates whether to save the card/token for future use.
		/// </summary>
		[JsonProperty("save")]
		public bool Save { get; set; }
	}

	/// <summary>
	/// Customer billing address.
	/// </summary>
	public class BillingAddress
	{
		/// <summary>
		/// First address line.
		/// </summary>
		[JsonProperty("address1")]
		public string Address1 { get; set; }

		/// <summary>
		/// Second address line.
		/// </summary>
		[JsonProperty("address2")]
		public string Address2 { get; set; }

		/// <summary>
		/// Third address line.
		/// </summary>
		[JsonProperty("address3")]
		public string Address3 { get; set; }

		/// <summary>
		/// City or locality.
		/// </summary>
		[JsonProperty("city")]
		public string City { get; set; }

		/// <summary>
		/// Country code (ISO 3166-1 alpha-2), e.g., GB, US.
		/// </summary>
		[JsonProperty("country")]
		public string Country { get; set; }

		/// <summary>
		/// Postal or ZIP code.
		/// </summary>
		[JsonProperty("postalCode")]
		public string PostalCode { get; set; }

		/// <summary>
		/// Helper for mapping addresses 
		/// </summary>
		/// <param name="address"></param>
		public BillingAddress(Address address)
		{
			Address1 = address.Line1;
			Address2 = address.Line2;
			Address3 = address.Line3;
			City = address.City;
			Country = address.CountryCode;
			PostalCode = address.Postcode;
		}
	}

	/// <summary>
	/// Recipient shipping details for delivery.
	/// </summary>
	public class ShippingDetails
	{
		/// <summary>
		/// Recipient's first name.
		/// </summary>
		[JsonProperty("recipientFirstName")]
		public string RecipientFirstName { get; set; }

		/// <summary>
		/// Recipient's last name.
		/// </summary>
		[JsonProperty("recipientLastName")]
		public string RecipientLastName { get; set; }

		/// <summary>
		/// First line of the shipping address.
		/// </summary>
		[JsonProperty("shippingAddress1")]
		public string ShippingAddress1 { get; set; }

		/// <summary>
		/// Second line of the shipping address.
		/// </summary>
		[JsonProperty("shippingAddress2")]
		public string ShippingAddress2 { get; set; }

		/// <summary>
		/// Third line of the shipping address.
		/// </summary>
		[JsonProperty("shippingAddress3")]
		public string ShippingAddress3 { get; set; }

		/// <summary>
		/// Shipping address city.
		/// </summary>
		[JsonProperty("shippingCity")]
		public string ShippingCity { get; set; }

		/// <summary>
		/// Shipping country code (ISO 3166-1 alpha-2).
		/// </summary>
		[JsonProperty("shippingCountry")]
		public string ShippingCountry { get; set; }

		/// <summary>
		/// Shipping postal or ZIP code.
		/// </summary>
		[JsonProperty("shippingPostalCode")]
		public string ShippingPostalCode { get; set; }

		/// <summary>
		/// Helper for mapping addresses 
		/// </summary>
		/// <param name="address"></param>
        public ShippingDetails(Address address) {
			ShippingAddress1 = address.Line1;
			ShippingAddress2 = address.Line2;
			ShippingAddress3 = address.Line3;
			ShippingCity = address.City;
			ShippingCountry = address.CountryCode;
			ShippingPostalCode = address.Postcode;
		}
	}

	/// <summary>
	/// Strong Customer Authentication (3-D Secure 2) context and browser data required by Opayo.
	/// </summary>
	public class StrongCustomerAuthentication
	{
		/// <summary>
		/// URL to receive SCA/3DS notifications.
		/// </summary>
		[JsonProperty("notificationURL")] 
		public string NotificationURL { get; set; }

		/// <summary>
		/// Customer browser IP address.
		/// </summary>
		[JsonProperty("browserIP")] 
		public string BrowserIP { get; set; }

		/// <summary>
		/// Browser Accept header value.
		/// </summary>
		[JsonProperty("browserAcceptHeader")] 
		public string BrowserAcceptHeader { get; set; }

		/// <summary>
		/// Indicates if JavaScript is enabled in the browser.
		/// </summary>
		[JsonProperty("browserJavascriptEnabled")] 
		public bool BrowserJavascriptEnabled { get; set; }

		/// <summary>
		/// Browser user agent string.
		/// </summary>
		[JsonProperty("browserUserAgent")] 
		public string BrowserUserAgent { get; set; }

		/// <summary>
		/// Challenge window size for 3DS (e.g., 250x400).
		/// </summary>
		[JsonProperty("challengeWindowSize")] 
		public string ChallengeWindowSize { get; set; }

		/// <summary>
		/// Transaction type indicator for 3DS.
		/// </summary>
		[JsonProperty("transType")] 
		public string TransType { get; set; }

		/// <summary>
		/// Browser language (BCP 47, e.g., en-GB).
		/// </summary>
		[JsonProperty("browserLanguage")] 
		public string BrowserLanguage { get; set; }

		/// <summary>
		/// Indicates if Java is enabled in the browser.
		/// </summary>
		[JsonProperty("browserJavaEnabled")] 
		public bool BrowserJavaEnabled { get; set; }

		/// <summary>
		/// Browser color depth.
		/// </summary>
		[JsonProperty("browserColorDepth")] 
		public string BrowserColorDepth { get; set; }

		/// <summary>
		/// Browser screen height in pixels.
		/// </summary>
		[JsonProperty("browserScreenHeight")] 
		public string BrowserScreenHeight { get; set; }

		/// <summary>
		/// Browser screen width in pixels.
		/// </summary>
		[JsonProperty("browserScreenWidth")] 
		public string BrowserScreenWidth { get; set; }

		/// <summary>
		/// Browser time zone offset (minutes from UTC).
		/// </summary>
		[JsonProperty("browserTZ")] 
		public string BrowserTZ { get; set; }

		/// <summary>
		/// Account ID or customer reference.
		/// </summary>
		[JsonProperty("acctID")] 
		public string AcctID { get; set; }

		/// <summary>
		/// 3DS exemption indicator if applicable.
		/// </summary>
		[JsonProperty("threeDSExemptionIndicator")] 
		public string ThreeDSExemptionIndicator { get; set; }

		/// <summary>
		/// Website or merchant domain.
		/// </summary>
		[JsonProperty("website")] 
		public string Website { get; set; }
	}

	/// <summary>
	/// Credential-on-file (COF) and Merchant Initiated Transaction (MIT) attributes.
	/// </summary>
	public class CredentialType
	{
		/// <summary>
		/// COF usage type (e.g., First, Subsequent).
		/// </summary>
		[JsonProperty("cofUsage")] 
		public string CofUsage { get; set; }

		/// <summary>
		/// Initiation type (CardholderInitiated or MerchantInitiated).
		/// </summary>
		[JsonProperty("initiatedType")] 
		public string InitiatedType { get; set; }

		/// <summary>
		/// MIT subtype (e.g., Unscheduled, Recurring, Installment).
		/// </summary>
		[JsonProperty("mitType")] 
		public string MitType { get; set; }
	}
}
