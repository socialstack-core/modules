using Api.Addresses;
using Api.Payments.Opayo.Response;
using Newtonsoft.Json;
using System.Collections.Generic;

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

		/// <summary>
		///List of supported payment methods the consume can select in HPP
		/// </summary>
		[JsonProperty("supportedPaymentMethods")]
		public Dictionary<string, SupportedPaymentMethod> SupportedPaymentMethods { get; set; }

	}

	/// <summary>
	/// Request to capture payment details via external page
	/// </summary>
	public class HostedPageRequest
	{
		/// <summary>
		/// Core payment details
		/// </summary>
		[JsonProperty("transactionDetails")]
		public TransactionRequest TransactionDetails { get; set; }

		/// <summary>
		/// Customer data capture settings.
		/// </summary>
		[JsonProperty("customerDataCapture")]
		public CustomerDataCapture CustomerDataCapture { get; set; }

		/// <summary>
		/// Optional presentation configuration for hosted payment pages.
		/// </summary>
		[JsonProperty("presentation")]
		public Presentation Presentation { get; set; }

		/// <summary>
		/// Use this object to specify desired behaviour after the transaction has completed.
		/// </summary>
		[JsonProperty("outcomeReport")]
		public OutcomeReport OutcomeReport { get; set; }

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
			if (!string.IsNullOrEmpty(address.Line2))
			{
				Address2 = address.Line2;
			}
			if (!string.IsNullOrEmpty(address.Line3))
			{
				Address3 = address.Line3;
			}
			if (string.IsNullOrEmpty(address.City))
			{
				City = address.Line3;
			}
			else
			{
				City = address.City;
			}
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
		public ShippingDetails(Address address)
		{
			ShippingAddress1 = address.Line1;
			if (!string.IsNullOrEmpty(address.Line2))
			{
				ShippingAddress2 = address.Line2;
			}
			if (!string.IsNullOrEmpty(address.Line3))
			{
				ShippingAddress3 = address.Line3;
			}
			if (string.IsNullOrEmpty(address.City))
			{
				ShippingCity = address.Line3;
			}
			else
			{
				ShippingCity = address.City;
			}
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
		/// Small = 250 x 400
		/// Medium = 390 x 400
		/// Large = 500 x 600
		/// ExtraLarge = 600 x 400
		/// FullScreen = Full screen        
		/// </summary>
		[JsonProperty("challengeWindowSize")]
		public string ChallengeWindowSize { get; set; }

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
		/// First = You are first storing a credential on file.
		/// Subsequent = You are using a stored credential
		/// </summary>
		[JsonProperty("cofUsage")]
		public string CofUsage { get; set; }

		/// <summary>
		/// Initiation type (CardholderInitiated or MerchantInitiated).
		/// CIT = Consumer Initiated Transaction also known as Cardholder Initiated Transaction. 3D Secure authentication is required unless it is a MOTO transaction.
		/// MIT = Merchant Initiated Transaction.The cofUsage:Subsequent must be submitted. 3D Secure authentication is not required.
		/// </summary>
		[JsonProperty("initiatedType")]
		public string InitiatedType { get; set; }

		/// <summary>
		/// MIT subtype (e.g., Unscheduled, Recurring, Installment).
		/// Instalment = A single purchase of goods/services paid for over multiple payments.
		/// Recurring = A purchase of goods/services provided at fixed regular intervals not exceeding one year between transactions.
		/// Unscheduled = A purchase of goods / services provided at irregular intervals with a fixed or variable amount.
		/// Incremental = An additional purchase made after an initial or estimated authorisation.Example; room service is added to the cardholders stay.Only available for certain MCCs, such as Hotels, Car Rental companies.
		/// DelayedCharge = An additional charge made after original services are rendered.Example; a parking fine.Only available for certain MCCs such as Car Rental companies.
		/// NoShow = A charge for services where the cardholder entered into an agreement to purchase, but did not meet the terms of the agreement.Example; A no show after booking a hotel room.Only available for certain MCCs, such as Hotels, Car Rental companies.
		/// Reauthorisation = A further purchase is made after the original purchase.Example; extended stays/rentals.Can also be used in split shipment scenarios.
		/// Resubmission = An authorisation request has been declined due to insufficient funds bankResponseCode:51, at the time the goods or services have already provided.You can resubmit your transaction to attempt to get a successful authorisation.       /// </summary>
		[JsonProperty("mitType")]
		public string MitType { get; set; }

		/// <summary>
		/// string 8 characters
		/// YYYYMMDD 
		/// Required if mitType:Recurring or mitType:Instalment. 
		/// This value relates to the date of when the last Recurring payment or Instalment will occur. If you submit a Recurring or Instalment transaction request after this date for the stored credential, the card issuer may decline the transaction or provide a 'soft decline'.
		/// </summary>
		[JsonProperty("recurringExpiry")]
		public string RecurringExpiry { get; set; }

		/// <summary>
		/// string 4 characters
		/// Value is in days. 
		/// Required if mitType:Recurring or mitType:Instalment. 
		/// The regular frequency of the Recurring payment or Instalment.
		/// </summary>
		[JsonProperty("recurringFrequency")]
		public string RecurringFrequency { get; set; }


		/// <summary>
		/// string 3 characters
		/// Value is in days.Required if mitType:Instalment.
		/// Must be a value greater than 1, example, 2 and upwards.
		/// The number of instalments required to fully pay off the received goods or services.
		/// Any extra instalments taken greater than the value entered, may lead to the card issuer declining the transaction request.
		/// </summary>
		[JsonProperty("purchaseInstalData")]
		public string PurchaseInstalData { get; set; }

	}

	/// <summary>
	/// Optional container for the field capture enabling flags. If not provided, the defaults detailed below will apply.
	/// </summary>
	public class CustomerDataCapture
	{
		/// <summary>
		/// Default : false
		/// If you wish to allow your customer to pay an alternative amount, pass true in this field. The amount supplied will be presented as an option on the hosted payment page.
		/// </summary>
		[JsonProperty("captureAmount")]
		public bool CaptureAmount { get; set; } = false;

		/// <summary>
		/// Default:false
		/// Default: false
		/// If this is set to true, then the customer's billing address fields will be requested on the payment form. 
		/// If you also provide the details in the billingAddress object, this data will prepopulate the billing address fields. 
		/// If this is set to false and the billing address object not provided, the transaction registration will fail.
		/// </summary>
		[JsonProperty("captureBillingAddress")]
		public bool CaptureBillingAddress { get; set; } = false;

		/// <summary>
		/// Default: false
		/// If this is set to true, then the customer's shipping address fields will be requested on the payment form. 
		/// If you also provide the details in the shippingAddress object, this data will prepopulate the shipping address fields.
		/// </summary>
		[JsonProperty("captureShippingAddress")]
		public bool CaptureShippingAddress { get; set; } = false;

		/// <summary>
		/// Default: false
		/// If this is set to true, then the customer's account details will be requested on the payment form. 
		/// If you provide these details in the transction registration post, these details will prepopulate the financial institution fields. 
		/// If your merchant category code is 6012, and if this is set to false, and you do not provide the fiRecipient fields, then transaction registration will fail.
		/// </summary>
		[JsonProperty("captureFiData")]
		public bool CaptureFiData { get; set; } = false;

		/// <summary>
		/// Default: false
		/// If this is set to true, then the customer's phone number will be requested on the hosted payment page. 
		/// If you provide these details in the transction registration post, these details will prepopulate the customer's phone number field.
		/// </summary>
		[JsonProperty("capturePhone")]
		public bool CapturePhone { get; set; } = false;

		/// <summary>
		/// Default: true
		/// If this is set to true, then the customer's email address will be requested on the hosted payment page. 
		/// If you provide these details in the transction registration post, these details will prepopulate the customer email fields
		/// </summary>
		[JsonProperty("captureEmail")]
		public bool CaptureEmail { get; set; } = false;
	}

	/// <summary>
	/// Presentation settings for the hosted payment journey.
	/// </summary>
	public class Presentation
	{
		/// <summary>
		/// Styling information for the hosted payment page.
		/// </summary>
		[JsonProperty("themeCustomisation")]
		public ThemeCustomisation ThemeCustomisation { get; set; }

		/// <summary>
		/// Visibility flags that control individual components on the page.
		/// </summary>
		[JsonProperty("componentVisibility")]
		public ComponentVisibility ComponentVisibility { get; set; }

		/// <summary>
		/// Language configuration, including supported locales and labels.
		/// </summary>
		[JsonProperty("language")]
		public LanguageSelection Language { get; set; }

		/// <summary>
		/// If this is provided, then the text provided in this field will be active as a link to this value. 
		/// If it is provided and displayTerms = false, this value will be ignored.
		/// </summary>
		[JsonProperty("termsLink")]
		public string TermsLink { get; set; }

		/// <summary>
		/// The domain that transactions will originate from. 
		/// Do not include any https:// or / characters.
		/// </summary>
		[JsonProperty("merchantDomain")]
		public string MerchantDomain { get; set; }


		/// <summary>
		/// Hosted payment page type selected for this transaction.
		/// </summary>
		[JsonProperty("paymentPageType")]
		public string PaymentPageType { get; set; } = "redirect";
	}

	/// <summary>
	/// Theme colour and branding configuration for hosted payments.
	/// </summary>
	public class ThemeCustomisation
	{
		/// <summary>
		/// Primary colour
		/// </summary>
		[JsonProperty("primaryColour")]
		public string PrimaryColour { get; set; }

		/// <summary>
		///  Secondary colour
		/// </summary>
		[JsonProperty("secondaryColour")]
		public string SecondaryColour { get; set; }

		/// <summary>
		///  submit colour
		/// </summary>
		[JsonProperty("submitColour")]
		public string SubmitColour { get; set; }
	}

	/// <summary>
	/// Visibility toggles for hosted payment page components.
	/// </summary>
	public class ComponentVisibility
	{
		/// <summary>
		/// Default: true
		///If this is set to true, then the hosted payment page will feature a language selector, which will translate text into other languages. 
		/// Please note, if you have provided custom button text this will not be translated - if your customer uses this function, button text will be translated to that the from the default text selection.
		/// </summary>
		[JsonProperty("displayLanguageSelector")]
		public bool DisplayLanguageSelector { get; set; }

		/// <summary>
		/// Default: false
		///If this is set to true, then the hosted payment page will display the card logos alongside card input fields (Visa, Mastercard, Amex, Discover/Diners, JCB as applicable on the vendor account)
		/// </summary>
		[JsonProperty("displayCardLogos")]
		public bool DisplayCardLogos { get; set; }

		/// <summary>
		/// Default: false
		/// If this is set to true, then the hosted payment page will display the vendor's logo on the payment page. 
		/// Default is false. 
		/// If the vendor does not have a logo uploaded to the Opayo system, then this field will be ignored.
		/// </summary>
		[JsonProperty("displayVendorLogo")]
		public bool DisplayVendorLogo { get; set; }

		/// <summary>
		/// Default: true
		/// If this is set to true, then the hosted payment page will display the supplied description.
		/// </summary>
		[JsonProperty("displayDescription")]
		public bool DisplayDescription { get; set; }

		/// <summary>
		/// Default: true
		/// If this is set to true, then the hosted payment page will display the supplied amount.
		/// </summary>
		[JsonProperty("displayAmount")]
		public bool DisplayAmount { get; set; }

		/// <summary>
		/// Default: false
		///If this is set to true, then the hosted payment page will display a link to the merchants terms and conditions (this will open in a new window).
		/// </summary>
		[JsonProperty("displayTerms")]
		public bool DisplayTerms { get; set; }
	}

	/// <summary>
	/// Language selection metadata for hosted payments.
	/// </summary>
	public class LanguageSelection
	{
		/// <summary>
		/// string
		/// Enum: "en" "fr" "de" "es"
		/// Language in which HPP will be opened
		/// </summary>
		[JsonProperty("preselectedLanguage")]
		public string PreselectedLanguage { get; set; }

		/// <summary>
		/// object (supportedLanguages)
		///List of supported languages which consumer can select in HPP
		/// </summary>
		[JsonProperty("supportedLanguages")]
		public Dictionary<string, SupportedLanguage> SupportedLanguages { get; set; }

		/// <summary>
		/// Enum: "en" "fr" "de" "es"
		/// List of supported languages which consumer can select in HPP (vie the selector)
		/// </summary>
		[JsonProperty("supportedLanguageList")]
		public string SupportedLanguageList { get; set; }
	}

	/// <summary>
	/// Locale-specific configuration for a supported language.
	/// </summary>
	public class SupportedLanguage
	{
		/// <summary>
		/// Is this language enabled 
		/// </summary>
		[JsonProperty("enabled")]
		public bool Enabled { get; set; }

		/// <summary>
		/// The labels for this language
		/// </summary>
		[JsonProperty("labels")]
		public LanguageLabels Labels { get; set; }
	}

	/// <summary>
	/// Label overrides for a specific language.
	/// </summary>
	public class LanguageLabels
	{
		/// <summary>
		/// Payment label
		/// </summary>
		[JsonProperty("pay")]
		public string Pay { get; set; }

		/// <summary>
		/// Cancel label 
		/// </summary>
		[JsonProperty("cancelPay")]
		public string CancelPay { get; set; }
	}

	/// <summary>
	/// Configuration for a supported payment type (card etc).
	/// </summary>
	public class SupportedPaymentMethod
	{
		/// <summary>
		/// Is this payment type enabled 
		/// </summary>
		[JsonProperty("enabled")]
		public bool Enabled { get; set; }

		/// <summary>
		/// Can the details for this payment method be saved for reuse
		/// </summary>
		[JsonProperty("enableSaveCard")]
		public bool EnableSaveCard { get; set; }
	}

	/// <summary>
	/// Use this object to specify desired behaviour after the transaction has completed.
	/// </summary>
	public class OutcomeReport
	{
		/// <summary>
		/// Urls the customer is redirected to after the transaction has completed.
		/// </summary>
		[JsonProperty("redirectUrls")]
		public RedirectUrls RedirectUrls { get; set; }

		/// <summary>
		/// Asynchronous notifications initiated after the transaction has completed.
		/// </summary>
		[JsonProperty("postProcessNotification")]
		public PostProcessNotification PostProcessNotification { get; set; }
	}

	/// <summary>
	///  Urls the customer is redirected to after the transaction has completed.
	/// </summary>
	public class RedirectUrls
	{
		/// <summary>
		/// string less or equal to 200 characters
		/// The merchant hosted URL to redirect to if the transaction is cancelled by the user.
		/// If this is not provided, in the event of a link being expired, your customer will be redirected to the failureURL.
		/// </summary>
		[JsonProperty("cancelUrl")]
		public string CancelUrl { get; set; }

		/// <summary>
		/// required
		/// string less or equal 200 characters
		/// The merchant hosted URL to redirect to if the transaction fails.Will have the transactionId field appended.
		/// </summary>
		[JsonProperty("failureUrl")]
		public string FailureUrl { get; set; }

		/// <summary>
		/// string less or equal 200 characters
		/// The merchant hosted URL to redirect to if the registered payment page has expired.
		/// Will have the transactionId field appended.
		/// If this is not provided, in the event of a link being expired, your customer will be redirected to the failureURL.
		/// </summary>
		[JsonProperty("expiryUrl")]
		public string ExpiryUrl { get; set; }

		/// <summary>
		/// required
		/// string less or equal 200 characters
		/// The merchant hosted URL to redirect to when the transaction is successfully authorised.Will have the transactionId field appended.
		/// </summary>
		[JsonProperty("successUrl")]
		public string SuccessUrl { get; set; }
	}

	/// <summary>
	/// Asynchronous notifications initiated after the transaction has completed.
	/// </summary>
	public class PostProcessNotification
	{
		/// <summary>
		/// Default: false
		/// Send email to the vendor
		/// </summary>
		[JsonProperty("sendVendorEmail")]
		public bool SendVendorEmail { get; set; } = false;

		/// <summary>
		/// Default: false
		/// If provided, send a email to the customer email address if provided.
		/// </summary>
		[JsonProperty("sendCustomerEmail")] 
		public bool SendCustomerEmail { get; set; } = false;
	}

}
