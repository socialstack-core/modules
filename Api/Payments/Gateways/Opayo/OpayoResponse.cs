using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace Api.Payments.Opayo.Response
{

	/// <summary>
	/// Opayo transaction response.
	/// </summary>
	public class TransactionResponse
	{
		/// <summary>
		/// High-level outcome code for the transaction.
		/// </summary>
		[JsonProperty("statusCode")]
		public string StatusCode { get; set; }

		/// <summary>
		/// Human-readable status details.
		/// </summary>
		[JsonProperty("statusDetail")]
		public string StatusDetail { get; set; }

		/// <summary>
		/// Opayo transaction type "Payment" "Deferred" "Authenticate" "Repeat" "Refund"
		/// </summary>
		[JsonProperty("transactionType")]
		public string TransactionType { get; set; }

		/// <summary>
		/// Opayo transaction identifier.
		/// </summary>
		[JsonProperty("transactionId")]
		public string TransactionId { get; set; }

		/// <summary>
		/// Access Control Server (ACS) URL for 3DS challenge.
		/// A fully qualified URL that points to the 3D Secure authentication system at the card holder's issuing bank
		/// </summary>
		[JsonProperty("acsUrl")]
		public string AcsUrl { get; set; }

		/// <summary>
		/// Access Control Server (ACS) transaction ID. 
		/// This is a unique ID provided by the card issuer for 3DSv2 authentications.
		/// </summary>
		[JsonProperty("acsTransId")]
		public string AcsTransId { get; set; }

		/// <summary>
		/// Directory Server (DS) transaction ID. 
		/// This is a unique ID provided by the card scheme for 3DSv2 authentications
		/// </summary>
		[JsonProperty("dsTransId")]
		public string DsTransId { get; set; }

		/// <summary>
		/// Overall transaction status
		///Ok - Transaction request completed successfully.
		///Registered - 3D Secure checks failed or were not performed, but the card details are still secured at Opayo. Only returned if the transactionType is Authenticate.
		///Authenticated - The 3D-Secure checks were performed successfully, and the card details secured at Opayo. Only returned if transactionType is Authenticate.
		///NotAuthed - Transaction request was not authorised by the bank.
		///Rejected - Transaction rejected by your fraud rules.
		///Malformed - Missing properties or badly formed body.
		///Invalid - Invalid property values supplied.
		///Error - An error occurred at Opayo.
		/// </summary>
		[JsonProperty("status")]
		public string Status { get; set; }

		/// <summary>
		/// 3DS challenge request payload (cReq) for browser redirection.
		/// A Base64 encoded message to be passed to the Issuing Bank as part of the 3D Secure Authentication. 
		/// This replaces the PAReq. When forwarding the cReq to the acsUrl, pass it in a field called creq (note the lower case cr). 
		/// This avoids issues at the ACS which expects the fieldname to be all lowercase.
		/// </summary>
		[JsonProperty("cReq")]
		public string CReq { get; set; }


		/// <summary>
		/// The VPSTxId is our unique transaction reference. 
		/// It is unique across all vendors using the Opayo systems and not only to you. 
		/// Always include the VPSTxId in any query you send to us about a transaction so we can respond accurately.
		/// </summary>
		[JsonProperty("vPSTxId")]
		public string VPSTxId { get; set; }

		/// <summary>
		/// The extended decline code detail which is returned by the card schemes. 
		/// Use this detail to ascertain if you can retry a transaction, need to contact your customer or if you should abandon all further attempts.
		/// </summary>
		[JsonProperty("additionalDeclineDetail")]
		public AdditionalDeclineDetail AdditionalDeclineDetail { get; set; }

		/// <summary>
		/// Opayo's unique Authorisation Code for a successfully authorised transaction. 
		/// Only present if status is Ok.
		/// </summary>
		[JsonProperty("retrievalReference")]
		public string RetrievalReference { get; set; }

		/// <summary>
		/// A unique reference that you may wish to be displayed on your acquirer's settlement report 
		/// Not enabled for all acquirers. 
		/// Please contact opayosupport@elavon.com for supported acquirers.
		/// </summary>
		[JsonProperty("settlementReferenceText")]
		public string SettlementReferenceText { get; set; }

		/// <summary>
		/// The bank response code returned by the card issuer.
		/// Also known as the decline code, these are codes that are specific to your merchant bank. 
		/// Please contact them for a description of each code. 
		/// If a bank response code of 65 or 1A is returned, this is known as a 'soft decline' and means the card issuer requests that your customer performs 3D Secure authentication, where they have to enter Two Factor Authentication (2FA) also known as Strong Customer Authentication (SCA). 
		/// To achieve this, submit a payment request and provide the field and value apply3DSecure:Force. 
		/// This is only returned for transaction type Payment
		/// </summary>
		[JsonProperty("bankResponseCode")]
		public string BankResponseCode { get; set; }

		/// <summary>
		/// The authorisation code returned from your merchant bank.
		/// </summary>
		[JsonProperty("bankAuthorisationCode")]
		public string BankAuthorisationCode { get; set; }

		/// <summary>
		/// The 3DSecure Details
		/// </summary>
		[JsonProperty("3DSecure")]
		public ThreeDSecure ThreeDSecure { get; set; }

		/// <summary>
		///	Information regarding the total, sale and surcharge amounts for the transaction. 
		/// The amount is only returned in response to GET requests to the transactions resource.
		/// </summary>
		[JsonProperty("amount")]
		public Amount Amount { get; set; }

		/// <summary>
		/// Information regarding the AVS/CV2 check results.
		/// </summary>
		[JsonProperty("avsCvcCheck")]
		public AvsCvcCheck AvsCvcCheck { get; set; }

		/// <summary>
		/// Payment method details such as card information.
		/// </summary>
		[JsonProperty("paymentMethod")]
		public PaymentMethodContainer PaymentMethod { get; set; }

		/// <summary>
		/// Financial Institute Recipient details
		/// </summary>
		[JsonProperty("fiRecipient")]
		public FiRecipient FiRecipient { get; set; }

		/// <summary>
		/// List of errors
		/// </summary>
		[JsonProperty("errors")]
		public List<OpayoError> Errors { get; set; }

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
	/// The credit or debit card details for this transaction.
	/// </summary>
	public class Card
	{
		/// <summary>
		/// The type of the card used for the transaction.
		/// </summary>
		[JsonProperty("cardType")]
		public string CardType { get; set; }

		/// <summary>
		/// The last 4 digits of the card number used for the transaction.
		/// </summary>
		[JsonProperty("lastFourDigits")]
		public string LastFourDigits { get; set; }

		/// <summary>
		/// The expiry date of the card used for the transaction.
		/// </summary>
		[JsonProperty("expiryDate")]
		public string ExpiryDate { get; set; }

		/// <summary>
		/// The unique reference of the card you want to charge.
		/// </summary>
		[JsonProperty("cardIdentifier")]
		public string CardIdentifier { get; set; }

		/// <summary>
		/// A flag to indicate the card identifier is reusable, i.e. it has been created previously.
		/// </summary>
		[JsonProperty("reusable")]
		public bool Reusable { get; set; }
	}


	/// <summary>
	/// The extended decline code detail which is returned by the card schemes. 
	/// Use this detail to ascertain if you can retry a transaction, need to contact your customer or if you should abandon all further attempts.
	/// </summary>
	public class AdditionalDeclineDetail
	{
		/// <summary>
		/// This contains the additional decline code. (example values, 03, R1, N4, 59 etc.)
		/// </summary>
		[JsonProperty("additionalDeclineCode")]
		public string AdditionalDeclineCode { get; set; }
		/// <summary>
		/// Description of the additional decline code. This will vary according to your acquiring bank
		/// </summary>
		[JsonProperty("additionalDeclineCodeDescription")]
		public string AdditionalDeclineCodeDescription { get; set; }

		/// <summary>
		/// This is the category of the decline. 
		/// Currently there are 4 categories, but could increase in future. (Example 01, 02, 03, 04.) 
		/// 01: Card declined, do not re-attempt within a rolling 30 day period. 
		/// 02: Card issuer cannot approve at this time. Re-attempt authorisation permitted, but limited to 15 re-attempts within a rolling 30 day period. 
		/// 03: Incorrect data, or updated or additional card account information is required. For these the authorisation data will need to be corrected before re-attempt. Such issues are incorrect / missing expiry date, authentication value. Contact cardholder and correct information. Note: Limited to 15 re-attempts within a rolling 30 day period and no more than 25,000 category 03 declines per MID per rolling 30 day period per card scheme. 
		/// 04: Re-attempt authorisation permitted. Re-attempts limited to 15 within a rolling 30 day period. example: '02'
		/// </summary>
		[JsonProperty("additionalDeclineCodeCategory")]
		public string AdditionalDeclineCodeCategory { get; set; }
	}


	/// <summary>
	/// The amount object provides information regarding the total, sale and surcharge amounts for the transaction. 
	/// The amount is only returned in response to GET requests to the transactions resource.
	/// </summary>
	public class Amount
	{
		/// <summary>
		/// The total amount for the transaction that includes any sale or surcharge values.
		/// </summary>
		[JsonProperty("totalAmount")]
		public int TotalAmount { get; set; }

		/// <summary>
		/// The sale amount associated with the cost of goods or services for the transaction.
		/// </summary>
		[JsonProperty("saleAmount")]
		public int SaleAmount { get; set; }

		/// <summary>
		/// The surcharge amount added to the transaction as per the settings of the account.
		/// </summary>
		[JsonProperty("surchargeAmount")]
		public int SurchargeAmount { get; set; }
	}

	/// <summary>
	/// The avsCvcCheck object provides information regarding the AVS/CV2 check results.
	/// </summary>
	public class AvsCvcCheck
	{
		/// <summary>
		/// The overall check result status. "AllMatched" "SecurityCodeMatchOnly" "AddressMatchOnly" "NoMatches" "NotChecked"
		/// </summary>
		[JsonProperty("status")]
		public string Status { get; set; }

		/// <summary>
		/// The result for address check. "Matched" "NotProvided" "NotChecked" "NotMatched"
		/// </summary>
		[JsonProperty("address")]
		public string Address { get; set; }
		/// <summary>
		/// The result for postal code check. "Matched" "NotProvided" "NotChecked" "NotMatched"
		/// </summary>
		[JsonProperty("postalCode")]
		public string PostalCode { get; set; }
		/// <summary>
		/// The result for security code check. "Matched" "NotProvided" "NotChecked" "NotMatched"
		/// </summary>
		[JsonProperty("securityCode")]
		public string SecurityCode { get; set; }
	}

	/// <summary>
	/// The fiRecipient object is required if your Merchant Category Code (MCC) has a value of 6012 (Financial Institution). 
	/// This is a card scheme mandated requirement. 
	/// If provided in the transaction request, the fiRecipient object and it's data will be returned in the transaction response.
	/// </summary>
	public class FiRecipient
	{
		/// <summary>
		/// Primary recipient's account number. This can either be:
		/// The first 6 and last 4 characters of the primary recipient's card PAN (no spaces).
		/// Where the primary recipient's account is not a card PAN; this will contain up to 10 characters of the account number (alphanumeric), unless the account number is less than 10 characters long, in which case the account number will be present in its entirety.
		/// </summary>
		[JsonProperty("accountNumber")]
		public string AccountNumber { get; set; }

		/// <summary>
		/// This is the surname of the primary recipient. No special characters such as apostrophes or hyphens are permitted.
		/// </summary>
		[JsonProperty("surname")]
		public string Surname { get; set; }

		/// <summary>
		/// This is the postcode of the primary recipient.
		/// </summary>
		[JsonProperty("postcode")]
		public string Postcode { get; set; }

		/// <summary>
		/// This is the date of birth of the primary recipient in the format YYYYMMDD.
		/// </summary>
		[JsonProperty("dateOfBirth")]
		public string DateOfBirth { get; set; }
	}

	/// <summary>
	/// The 3DSecure Details
	/// </summary>
	public class ThreeDSecure
	{
		/// <summary>
		///The 3D Secure status of the transaction, if applied.
		///Ok - Transaction request completed successfully.
		///Authenticated - 3D Secure checks carried out and user authenticated correctly.
		///NotChecked - 3D Secure checks were not performed. This indicates that 3D Secure was either switched off at the account level, or disabled at transaction registration with the apply3DSecure set to Disable.
		///NotAuthenticated - 3D Secure authentication checked, but the user failed the authentication.
		///Error - Authentication could not be attempted due to data errors or service unavailability in one of the parties involved in the check.	
		///CardNotEnrolled - This means that the card is not in the 3D Secure scheme.
		///IssuerNotEnrolled - This means that the issuer is not part of the 3D Secure scheme.
		///MalformedOrInvalid - This means that there is a problem with creating or receiving the 3D Secure data. These should not occur on the live environment.
		///AttemptOnly - This means that the cardholder attempted to authenticate themselves but the process did not complete. A liability shift may occur for non-Maestro cards, depending on your merchant agreement.
		///Incomplete - This means that the 3D Secure authentication was not available (normally at the card issuer site).
		/// </summary>

		[JsonProperty("status")]
		public string Status { get; set; }
	}

	/// <summary>
	/// Opayo Merchant session key response
	/// </summary>
	public class MerchantSessionKeyResponse
	{
		/// <summary>
		/// The expiry date of the merchant session key.
		/// </summary>
		public DateTime Expiry { get; set; }

		/// <summary>
		/// The merchant session key.
		/// </summary>
		public string MerchantSessionKey { get; set; }
	}


	/// <summary>
	/// Exception thrown for payo API errors with attached context.
	/// </summary>
	public class OpayoApiException : Exception
	{
		/// <summary>
		/// The HTTP status code of the response.
		/// </summary>
		public HttpStatusCode StatusCode { get; }

		/// <summary>
		/// Parsed Opayo error detail, if available.
		/// </summary>
		public OpayoError Error { get; }


		/// <summary>
		/// Creates a new <see cref="OpayoApiException"/>.
		/// </summary>
		public OpayoApiException(HttpStatusCode statusCode, string message, OpayoError error = null)
			: base(message) => (StatusCode, Error) = (statusCode, error);
	}

	/// <summary>
	/// Opayo errors response
	/// </summary>
	public class OpayoErrors
	{
		/// <summary>
		/// List of errors
		/// </summary>
		public List<OpayoError> Errors { get; set; }
	}

	/// <summary>
	/// Opayo error response
	/// </summary>
	public class OpayoError
	{
		/// <summary>
		/// The error code
		/// </summary>
		public string Code { get; set; }

		/// <summary>
		/// The error property
		/// </summary>
		public string Property { get; set; }

		/// <summary>
		/// The error description
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Summary of error used in exception
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{Code}:{Property}:{Description}";

	}

}
