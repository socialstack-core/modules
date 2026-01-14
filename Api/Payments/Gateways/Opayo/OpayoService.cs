using System.Threading.Tasks;
using Api.Contexts;
using Api.Users;
using System;
#if PAYMENTS_GUEST_USERS
using Api.GuestUsers;
#endif
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Api.Startup;
using Api.Addresses;
using Api.Configuration;
using Api.Database;

namespace Api.Payments
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// https://developer.elavon.com/products/en-uk/opayo/v1/integration-options-byopp
	/// https://developer.elavon.com/products/en-uk/opayo/v1/api-reference#tag/Instructions/operation/createInstruction
	/// https://developer.elavon.com/products/en-uk/opayo-server/v1/testing-4513
	/// https://developer.elavon.com/products/en-uk/opayo/v1/3DS-authentication
	/// See repo for postman collection
	/// </summary>
	public partial class OpayoService : AutoService
	{
		private OpayoGateway _gateway;
		private OpayoConfig _config;
		private PurchaseTokenService _purchaseTokens;

		private readonly static Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => new HttpClient());
		private readonly JsonSerializerSettings _json;

		private HttpClient Http { get => _httpClient.Value; }

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public OpayoService(PaymentGatewayService gateways, PurchaseTokenService purchaseTokens)
		{
			_purchaseTokens = purchaseTokens;

			// Get configuration:
			var opayoConfig = GetConfig<OpayoConfig>();

			if (string.IsNullOrWhiteSpace(opayoConfig.IntegrationKey))
			{
				// Not configured - don't register Opayo.
				return;
			}

			Log.Info(LogTag, "Registering Opayo Payment Gateway");

			_config = opayoConfig;

			_json = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				MissingMemberHandling = MissingMemberHandling.Ignore,
				DateParseHandling = DateParseHandling.DateTimeOffset,
			};

			_gateway = new OpayoGateway(opayoConfig, this);

			gateways.Register(_gateway);
			_purchaseTokens = purchaseTokens;
		}

		/// <summary>
		/// The current opayo config. It's null if opayo is not registered.
		/// </summary>
		public OpayoConfig Config => _config;

		/// <summary>
		/// Get a merchant session key
		/// </summary>
		/// <returns></returns>
		public async ValueTask<Opayo.Response.MerchantSessionKeyResponse> GetMerchantSessionKey()
		{
			// Create request body (JSON)
			var body = new
			{
				vendorName = _config.VendorName
			};

			return await Post<Opayo.Response.MerchantSessionKeyResponse>("merchant-session-keys", body);
		}

		/// <summary>
		/// Converts intent status to a Purchase.Status ID.
		/// </summary>
		/// <param name="status"></param>
		/// <returns></returns>
		public uint ConvertStatus(string status)
		{
			if (status == "Malformed" || status == "Invalid")
			{
				// Failed (frontend failure)
				return 400;
			}
			else if (status == "NotAuthed" || status == "Rejected")
			{
				// Rejected at gateway.
				return 401;
			}
			else if (status == "Registered" || status == "Authenticated")
			{
				// Pending at gateway.
				return 102;
			}
			else if (status == "requires_action")
			{
				// Pending but requires user to go to an action URL.
				return 103;
			}
			else if (status == "3DAuth")
			{
				// Payment approval requird via 3d auth
				return 250;
			}
			else if (status == "Ok")
			{
				// Payment succeeded.
				return 202;
			}
			else
			{
				// Failed. Unknown error state.
				return 500;
			}
		}

		/// <summary>
		/// The opayo payment gateway.
		/// </summary>
		public class OpayoGateway : PaymentGateway
		{
			/// <summary>
			/// Creates a opayo gateway.
			/// </summary>
			/// <param name="config"></param>
			/// <param name="opayo"></param>
			public OpayoGateway(OpayoConfig config, OpayoService opayo)
			{
				_config = config;
				_opayo = opayo;
				Id = 2;
			}

			/// <summary>
			/// The config for the gateway.
			/// </summary>
			private OpayoConfig _config;

			private UserService _users;
#if PAYMENTS_GUEST_USERS
			private GuestUserService _guests;
#endif
			private PurchaseService _purchases;
			private PurchaseTokenService _purchaseTokens;
			private AddressService _addresses;
			private OpayoService _opayo;
			private PaymentMethodService _paymentMethods;

			/// <summary>
			/// Process the response from a 3DSecure challenge 
			/// </summary>
			/// <param name="purchase"></param>
			/// <param name="challengeResponse"></param>
			/// <returns></returns>
			public override async ValueTask<PurchaseAndAction> ValidateChallenge(Purchase purchase, ChallengeResponse challengeResponse)
			{
				var context = new Context(1, 0, 1);

				if (_users == null)
				{
					_users = Services.Get<UserService>();
					_purchases = Services.Get<PurchaseService>();
					_purchaseTokens = Services.Get<PurchaseTokenService>();
					_paymentMethods = Services.Get<PaymentMethodService>();
#if PAYMENTS_GUEST_USERS
					_guests = Services.Get<GuestUserService>();
#endif
					_addresses = Services.Get<AddressService>();
				}

				// Create request body (JSON)
				var body = new
				{
					cRes = challengeResponse.CRes
				};

				var transationResponse = await _opayo.Post<Opayo.Response.TransactionResponse>($"transactions/{purchase.PaymentGatewayInternalId}/3d-secure-challenge", body);

				if (transationResponse == null)
				{
					throw new PublicException("Failed to process payment tranation.", "Purchase_payment_transaction");
				}

				// Update purchase latest status
				purchase = await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
				{
					toUpdate.Status = _opayo.ConvertStatus(transationResponse.Status);
					toUpdate.GatewayResponseJson = new JsonString(_purchases.AppendResponseElement(orig.GatewayResponseJson.ValueOf(), transationResponse));
				});

				return new PurchaseAndAction()
				{
					Purchase = purchase
				};
			}


			/// <summary>
			/// Asks the payment gateway to complete a purchase.
			/// </summary>
			/// <param name="purchase"></param>
			/// <param name="totalCost"></param>
			/// <param name="paymentMethod"></param>
			/// <returns></returns>
			public override async ValueTask<PurchaseAndAction> ExecutePurchase(Purchase purchase, ProductCost totalCost, PaymentMethod paymentMethod)
			{
				if (_config.VerboseLogging)
				{
					Log.Info("OpayoService", $"Opayo - Processing transation {purchase.Reference}");
				}

				var context = new Context(1, 0, 1);

				if (_users == null)
				{
					_users = Services.Get<UserService>();
					_purchases = Services.Get<PurchaseService>();
					_purchaseTokens = Services.Get<PurchaseTokenService>();
					_paymentMethods = Services.Get<PaymentMethodService>();
#if PAYMENTS_GUEST_USERS
					_guests = Services.Get<GuestUserService>();
#endif
					_addresses = Services.Get<AddressService>();
				}

				// Get the payment method:
				if (paymentMethod == null || string.IsNullOrEmpty(paymentMethod.GatewayToken))
				{
					throw new PublicException("The provided payment method is invalid.", "invalid_payment_method");
				}

				if (totalCost.Amount >= long.MaxValue)
				{
					// Long cast overflow check:
					throw new PublicException("Requested quantity is too large.", "substantial_quantity");
				}

				// Mark as starting to submit to gateway and add the total cost to it:
				await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
				{
					// It might have instantly completed or instantly failed. We can find out from the status:
					toUpdate.Status = 101;
					toUpdate.TotalCost = totalCost.Amount;
					toUpdate.TotalCostLessTax = totalCost.AmountLessTax;
					toUpdate.CurrencyCode = totalCost.CurrencyCode;
				});

				Uri callbackUrl = null;
				if (!string.IsNullOrWhiteSpace(_config.CallbackUrl))
				{
					callbackUrl = new Uri(_config.CallbackUrl);
				} else
				{
					callbackUrl = new Uri(AppSettings.GetPublicUrl(1) + "/v1/purchase/opayo/challenge/callback");
				}

				var paymentRequest = new Opayo.Request.TransactionRequest()
				{
					TransactionType = "Payment",
					VendorName = _config.VendorName,
					VendorTxCode = purchase.Reference,
					EntryMethod = "Ecommerce",
#if PAYMENTS_GUEST_USERS
					Description = "Ecommerce purchase" + (purchase.GuestUserId > 0 ? " (guest)" : ""),
#else
					Description = "Ecommerce purchase",
#endif
					Amount = (int)totalCost.Amount,
					Apply3DSecure = _config.Apply3DSecure,
					ApplyAvsCvcCheck = _config.ApplyAvsCvcCheck,
					Currency = totalCost.CurrencyCode,
					PaymentMethod = new Opayo.Request.PaymentMethodContainer()
					{
						Card = new Opayo.Request.Card()
						{
							CardIdentifier = paymentMethod.GatewayToken,
							MerchantSessionKey = paymentMethod.SessionId,
							Reusable = false,
							Save = false
						}
					},
					StrongCustomerAuthentication = new Opayo.Request.StrongCustomerAuthentication()
					{
						Website = callbackUrl.GetLeftPart(UriPartial.Authority),
						NotificationURL = callbackUrl.ToString(),
						BrowserAcceptHeader = "text/html, application/json",
						ChallengeWindowSize = "Small",
						TransType = "GoodsAndServicePurchase",
						BrowserJavascriptEnabled = paymentMethod.BrowserInfo.BrowserJavascriptEnabled,
						BrowserJavaEnabled = paymentMethod.BrowserInfo.BrowserJavaEnabled,
						BrowserTZ = paymentMethod.BrowserInfo.BrowserTZ,
						BrowserColorDepth = paymentMethod.BrowserInfo.BrowserColorDepth,
						BrowserLanguage = paymentMethod.BrowserInfo.BrowserLanguage,
						BrowserScreenHeight = paymentMethod.BrowserInfo.BrowserScreenHeight,
						BrowserScreenWidth = paymentMethod.BrowserInfo.BrowserScreenWidth,
						BrowserUserAgent = paymentMethod.BrowserInfo.BrowserUserAgent,
						ThreeDSExemptionIndicator = "LowValue",
						BrowserIP = purchase.IpAddress
					}
				};

				if (purchase.UserId > 0)
				{
					var user = await _users.Get(context, purchase.UserId, DataOptions.IgnorePermissions);
					if (user != null)
					{
						paymentRequest.CustomerFirstName = user.FirstName;
						paymentRequest.CustomerLastName = user.LastName;
						paymentRequest.CustomerEmail = user.Email;
						paymentRequest.CustomerPhone = user.PhoneNumber;
					}
					else
					{
						throw new PublicException("Could not locate user.", "purchase_user");
					}
				}
#if PAYMENTS_GUEST_USERS
				else if (purchase.GuestUserId.GetValueOrDefault() > 0)
				{
					var guestUser = await _guests.Get(context, purchase.GuestUserId.GetValueOrDefault(), DataOptions.IgnorePermissions);
					if (guestUser != null)
					{
						paymentRequest.CustomerFirstName = guestUser.FirstName;
						paymentRequest.CustomerLastName = guestUser.LastName;
						paymentRequest.CustomerEmail = guestUser.Email;
					}
					else
					{
						throw new PublicException("Could not locate guest user.", "purchase_guest_user");
					}
				}
#endif
				else
				{
					throw new PublicException("Could not locate purchase user.", "purchase_user");
				}

				if (purchase.BillingAddressId != 0)
				{
					var address = await _addresses.Get(context, purchase.BillingAddressId, DataOptions.IgnorePermissions);
					if (address == null)
					{
						throw new PublicException("Could not locate billing address.", "Purchase_billing_address");
					}

					paymentRequest.BillingAddress = new Opayo.Request.BillingAddress(address);
					paymentRequest.CustomerPhone = Opayo.PhoneNumberFormatter.FormatUk(address.TelNo);

					paymentRequest.CustomerWorkPhone = Opayo.PhoneNumberFormatter.FormatUk(address.TelNo);
				}

				if (purchase.DeliveryAddressId != 0)
				{
					var address = await _addresses.Get(context, purchase.DeliveryAddressId, DataOptions.IgnorePermissions);
					if (address == null)
					{
						throw new PublicException("Could not locate delivery address.", "Purchase_delivery_address");
					}

					paymentRequest.ShippingDetails = new Opayo.Request.ShippingDetails(address);
					paymentRequest.ShippingDetails.RecipientFirstName = paymentRequest.CustomerFirstName;
					paymentRequest.ShippingDetails.RecipientLastName = paymentRequest.CustomerLastName;

					paymentRequest.CustomerPhone = Opayo.PhoneNumberFormatter.FormatUk(address.TelNo);

					if (purchase.BillingAddressId == 0)
					{
						paymentRequest.BillingAddress = new Opayo.Request.BillingAddress(address);
					}
				}

				var transationResponse = await _opayo.Post<Opayo.Response.TransactionResponse>("transactions", paymentRequest);

				if (transationResponse == null)
				{
					throw new PublicException("Failed to process payment tranation.", "Purchase_payment_transaction");
				}

				// Update purchase with Id from payment intent and the total cost:
				purchase = await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
				{
					// It might have instantly completed or instantly failed. We can find out from the status:
					toUpdate.Status = _opayo.ConvertStatus(transationResponse.Status);
					toUpdate.PaymentGatewayInternalId = transationResponse.TransactionId;
					toUpdate.GatewayResponseJson = new JsonString(_purchases.AppendResponseElement(orig.GatewayResponseJson.ValueOf(), transationResponse));
				});

				if (_config.VerboseLogging)
				{
					Log.Info("OpayoService", $"Opayo - Processed transation {purchase.Reference} {purchase.Status} {transationResponse.Status} {transationResponse.StatusDetail}");
				}

				var purchaseAction = new PurchaseAndAction()
				{
					Purchase = purchase
				};

				if (purchase.Status == 250) // requires additional challenge
				{
					if (!string.IsNullOrWhiteSpace(transationResponse.AcsUrl) && !string.IsNullOrWhiteSpace(transationResponse.CReq))
					{
						//https://developer.elavon.com/products/en-uk/opayo/v1/3DS-authentication

						// create a short term token to allow anon access to check status
						var token = await _purchaseTokens.Create(context,
							new PurchaseToken()
							{
								PurchaseId = purchase.Id,
								Scope = "Authentication",
								IsSingleUse = false,
								CreatedUtc = DateTime.UtcNow,
								ExpiresUtc = DateTime.UtcNow.AddMinutes(15)
							}, DataOptions.IgnorePermissions);

						if (token == null)
						{
							throw new PublicException("Could not create session token.", "Purchase_session_token");
						}

						purchaseAction.MetaData = new ChallengeMetaData()
						{
							// need to encode the token as it's also passed to the transation verification service
							SessionToken = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Token))),
							Token = token.Token,
							ChallengeRequest = transationResponse.CReq,
							ChallengeUrl = transationResponse.AcsUrl
						};
					}
				}
				else if (purchase.Status >= 200 && purchase.Status < 300)
				{
#if PAYMENTS_GUEST_USERS
					if (purchase.GuestUserId.GetValueOrDefault() > 0)
					{
						// create a short term token to allow anon access to view completed purchase
						var token = await _purchaseTokens.Create(context,
						new PurchaseToken()
						{
							PurchaseId = purchase.Id,
							Scope = "View",
							IsSingleUse = false,
							CreatedUtc = DateTime.UtcNow,
							ExpiresUtc = DateTime.UtcNow.AddDays(14)
						}, DataOptions.IgnorePermissions);

						if (token == null)
						{
							throw new PublicException("Could not create session token.", "Purchase_session_token");
						}

						purchaseAction.MetaData = new ChallengeMetaData()
						{
							Token = token.Token
						};
					}
#endif
				}

				return purchaseAction;
			}
		}

		/// <summary>
		/// Make a post request to the opayo api
		/// </summary>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private async ValueTask<T> Post<T>(string endpoint, object data)
		{
			var json = JsonConvert.SerializeObject(data, _json);

			//todo remove this once tested
			if (_config.VerboseLogging)
			{
				Log.Info(LogTag, $"DEBUG - Opayo Request : '{endpoint} {json}'");
			}

			if (!Uri.TryCreate(_config.RestBaseURL, UriKind.Absolute, out _))
			{
				Log.Warn(LogTag, $"Invalid base URL: '{_config.RestBaseURL}'");
				return default;
			}

			string requestUrl = $"{_config.RestBaseURL.TrimEnd('/')}/{endpoint}";
			var requestUri = new Uri(requestUrl);

			var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};

			var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.IntegrationKey}:{_config.IntegrationPassword}"));
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var response = await Http.SendAsync(request);
			return await ReadOrThrow<T>(response);
		}

		/// <summary>
		/// Read the response as <typeparamref name="T"/> 
		/// or 
		/// throw a <see cref="Opayo.Response.OpayoApiException"/> with parsed problem details.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="resp"></param>
		/// <returns></returns>
		/// <exception cref="Opayo.Response.OpayoApiException"></exception>
		private async ValueTask<T> ReadOrThrow<T>(HttpResponseMessage resp)
		{
			var text = await resp.Content.ReadAsStringAsync();

			//todo remove this once tested
			if (_config.VerboseLogging)
			{
				Log.Info(LogTag, $"DEBUG - Opayo Response : '{resp.StatusCode} {text}'");
			}

			if (resp.IsSuccessStatusCode)
			{
				var ok = JsonConvert.DeserializeObject<T>(text, _json);
				if (ok is null)
				{
					throw new Opayo.Response.OpayoApiException(resp.StatusCode, "Empty/invalid JSON payload.");
				}
				return ok;
			}

			if (resp.StatusCode == System.Net.HttpStatusCode.UnprocessableContent)
			{
				// The request was well-formed but contains invalid values or missing properties.
				var ok = JsonConvert.DeserializeObject<T>(text, _json);
				if (ok is null)
				{
					throw new Opayo.Response.OpayoApiException(resp.StatusCode, "Empty/invalid JSON payload.");
				}
				return ok;
			}

			Opayo.Response.OpayoErrors errors = null;
			try
			{
				errors = JsonConvert.DeserializeObject<Opayo.Response.OpayoErrors>(text, _json);
			}
			catch
			{
				/* ignore */
			}

			Opayo.Response.OpayoError error = null;
			try
			{
				error = JsonConvert.DeserializeObject<Opayo.Response.OpayoError>(text, _json);
			}
			catch
			{
				/* ignore */
			}

			var msg = "";
			if (errors != null && errors.Errors.Count > 0)
			{
				foreach (var err in errors.Errors)
				{
					msg += err.ToString() + "::";
				}

				error = errors.Errors[0];

			}
			else if (error != null)
			{
				msg = error.ToString() + "::";
			}

			msg += $"{(int)resp.StatusCode} {resp.ReasonPhrase}::{text}";
			throw new Opayo.Response.OpayoApiException(resp.StatusCode, msg, error);
		}
	}
}
