using Api.Addresses;
using Api.Configuration;
using Api.Contexts;
using Api.Database;
using Api.Eventing;

#if PAYMENTS_GUEST_USERS
using Api.GuestUsers;
#endif
using Api.Payments.Opayo.Request;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

			if (!opayoConfig.IsEnabled)
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

			Events.Healthz.RunDependencyChecks.AddEventListener(async (Context context, HealthzChecks checks) =>
			{
				var opayoOk = false;
				string opayoMessage = null;

				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
				try
				{
					var body = new
					{
						vendorName = _config.VendorName
					};

					var merchantKey = await Post<Opayo.Response.MerchantSessionKeyResponse>("merchant-session-keys", body, cts.Token);

					if (merchantKey != null && ! string.IsNullOrWhiteSpace(merchantKey.MerchantSessionKey))
					{
						opayoOk = true;
					}
					else
					{
						opayoMessage = "Failed to retrieve session details";
					}
				}
				catch (OperationCanceledException)
				{
					opayoMessage = "Failed to connect :: timed out after 10 seconds";
				}
				catch (Exception)
				{
					opayoMessage = "Failed to connect";
				}

				checks.opayo = new { ok = opayoOk, message = opayoMessage };

				if (!opayoOk)
				{
					checks.Ok = false;
				}

				return checks;
			});


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
				return 300;
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
				ShortCode = _config.ShortCode;
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
					throw new PublicException("Failed to process payment transaction.", "opayo/missing_transation");
				}

				// Update purchase latest status
				purchase = await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
				{
					toUpdate.Status = _opayo.ConvertStatus(transationResponse.Status);
					toUpdate.GatewayResponseJson = new JsonString(_purchases.AppendResponseElement(orig.GatewayResponseJson.ValueOf(), transationResponse));
					toUpdate.GatewayPublicJson = new JsonString(JsonConvert.SerializeObject(_opayo.GetTransactionMessage(transationResponse)));
				});

				return new PurchaseAndAction()
				{
					Purchase = purchase
				};
			}

			/// <summary>
			/// Process the response from a hosted page transaction 
			/// 
			/// https://{hostname}/cart/purchases/hosted/success/NzFVcnZFbFFNeENPaFFmMkEyTDU%253D?
			/// transactionId=6C3E78AC-08B8-AB32-B900-3A6099FBA009&amp;
			/// vendorTxCode=WEB-03371&amp;
			/// registrationId=3a392ece-c59d-4683-802d-1f5d886b0e1f&amp;
			/// expiry=2026-02-17T09:59:55.009Z&amp;
			/// state=success&amp;
			/// signature=G_Hv_X-_vw4khfBa6v5fJmnc6tM20pIZFEe0oEmYHSY%3D
			/// 
			/// </summary>
			/// <param name="context"></param>
			/// <param name="purchase"></param>
			/// <param name="hostedPageResponse"></param>
			/// <returns></returns>
			public override async ValueTask<Purchase> ValidateHostedPageTransaction(Context context, Purchase purchase, HostedPageResponse hostedPageResponse)
			{
				if (_users == null)
				{
					_users = Services.Get<UserService>();
					_purchases = Services.Get<PurchaseService>();
					_purchaseTokens = Services.Get<PurchaseTokenService>();
#if PAYMENTS_GUEST_USERS
					_guests = Services.Get<GuestUserService>();
#endif
					_addresses = Services.Get<AddressService>();
				}

				if (!string.IsNullOrWhiteSpace(hostedPageResponse.TransactionId) && hostedPageResponse.Status == "success")
				{
					var transationResponse = await _opayo.Get<Opayo.Response.TransactionResponse>($"transactions/{hostedPageResponse.TransactionId}");
					if (transationResponse == null)
					{
						return null;
					}

					purchase = await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
					{
						toUpdate.Status = _opayo.ConvertStatus(transationResponse.Status);
						toUpdate.GatewayPublicJson = new JsonString(null);
						toUpdate.PaymentGatewayInternalId = hostedPageResponse.TransactionId;
					}, DataOptions.IgnorePermissions);
				}
				else if (hostedPageResponse.Status == "cancel")
				{
					purchase = await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
					{
						toUpdate.GatewayPublicJson = new JsonString(JsonConvert.SerializeObject(hostedPageResponse.Status));
						toUpdate.Status = 402;
					}, DataOptions.IgnorePermissions);
				}
				else
				{
					purchase = await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
					{
						toUpdate.GatewayPublicJson = new JsonString(JsonConvert.SerializeObject(hostedPageResponse.Status));
						toUpdate.Status = 401;
					}, DataOptions.IgnorePermissions);
				}

				return purchase;
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
#if PAYMENTS_GUEST_USERS
					_guests = Services.Get<GuestUserService>();
#endif
					_addresses = Services.Get<AddressService>();
				}

				// Get the payment method:
				if (paymentMethod == null || string.IsNullOrEmpty(paymentMethod.GatewayToken))
				{
					throw new PublicException("The provided payment method is invalid.", "opaypo/invalid_payment_method");
				}

				if (totalCost.Amount >= long.MaxValue)
				{
					// Long cast overflow check:
					throw new PublicException("Requested quantity is too large.", "opaypo/substantial_quantity");
				}

				// Mark as starting to submit to gateway and add the total cost to it:
				purchase = await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
				{
					// It might have instantly completed or instantly failed. We can find out from the status:
					toUpdate.Status = 101;
					toUpdate.TotalCost = totalCost.Amount;
					toUpdate.TotalCostLessTax = totalCost.AmountLessTax;
					toUpdate.CurrencyCode = totalCost.CurrencyCode;
				});

				var callbackUrl = new Uri(AppSettings.GetPublicUrl(context.LocaleId) + "/v1/purchase/opayo/challenge/callback");

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
					Currency = totalCost.CurrencyCode
				};

				if (!_config.HostedPageEnabled)
				{
					paymentRequest.Apply3DSecure = _config.Apply3DSecure;
					paymentRequest.ApplyAvsCvcCheck = _config.ApplyAvsCvcCheck;

					paymentRequest.PaymentMethod = new Opayo.Request.PaymentMethodContainer()
					{
						Card = new Opayo.Request.Card()
						{
							CardIdentifier = paymentMethod.GatewayToken,
							MerchantSessionKey = paymentMethod.SessionId,
							Reusable = false,
							Save = false
						}
					};

					paymentRequest.StrongCustomerAuthentication = new Opayo.Request.StrongCustomerAuthentication()
					{
						Website = callbackUrl.GetLeftPart(UriPartial.Authority),
						NotificationURL = callbackUrl.ToString(),
						BrowserAcceptHeader = "text/html, application/json",
						ChallengeWindowSize = "Small",
						TransType = _config.TransType,
						BrowserJavascriptEnabled = paymentMethod.BrowserInfo.BrowserJavascriptEnabled,
						BrowserJavaEnabled = paymentMethod.BrowserInfo.BrowserJavaEnabled,
						BrowserTZ = paymentMethod.BrowserInfo.BrowserTZ,
						BrowserColorDepth = paymentMethod.BrowserInfo.BrowserColorDepth,
						BrowserLanguage = paymentMethod.BrowserInfo.BrowserLanguage,
						BrowserScreenHeight = paymentMethod.BrowserInfo.BrowserScreenHeight,
						BrowserScreenWidth = paymentMethod.BrowserInfo.BrowserScreenWidth,
						BrowserUserAgent = paymentMethod.BrowserInfo.BrowserUserAgent,
						ThreeDSExemptionIndicator = string.IsNullOrWhiteSpace(_config.ThreeDSExemptionIndicator) ? null : _config.ThreeDSExemptionIndicator,
						BrowserIP = purchase.IpAddress
					};
				}

				if (purchase.UserId > 0)
				{
					var user = await _users.Get(context, purchase.UserId, DataOptions.IgnorePermissions);
					if (user != null)
					{
						paymentRequest.CustomerFirstName = user.FirstName;
						paymentRequest.CustomerLastName = user.LastName;

						paymentRequest.CustomerEmail = user.Email;

						var phoneNumber = Opayo.PhoneNumberFormatter.FormatUk(user.PhoneNumber);

						if (!string.IsNullOrWhiteSpace(phoneNumber))
						{
							paymentRequest.CustomerPhone = phoneNumber;
							paymentRequest.CustomerWorkPhone = phoneNumber;
						}
					}
					else
					{
						throw new PublicException("Could not locate user.", "opaypo/missing_user");
					}

					var requestToSaveCard = paymentMethod.Id > 0 && !paymentMethod.IsValidated;
					var reusingExistingCard = paymentMethod.Id > 0 && paymentMethod.IsValidated;

					if (requestToSaveCard || reusingExistingCard)
					{
						paymentRequest.CredentialType = new CredentialType()
						{
							CofUsage = requestToSaveCard ? "First" : "Subsequent",
							InitiatedType = "CIT",
							MitType = "Unscheduled"
						};

						if (requestToSaveCard)
						{
							paymentRequest.Apply3DSecure = "Force";
							paymentRequest.PaymentMethod.Card.Save = true;
						}
						else
						{
							paymentRequest.PaymentMethod.Card.Reusable = true;
						}
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
						throw new PublicException("Could not locate guest user.", "opaypo/missing_guest_user");
					}
				}
#endif
				else
				{
					throw new PublicException("Could not locate purchase user.", "opaypo/missing_user");
				}

				if (purchase.BillingAddressId != 0)
				{
					var address = await _addresses.Get(context, purchase.BillingAddressId, DataOptions.IgnorePermissions);
					if (address == null)
					{
						throw new PublicException("Could not locate billing address.", "opaypo/missing_billing_address");
					}

					paymentRequest.BillingAddress = new Opayo.Request.BillingAddress(address);

					var phoneNumber = Opayo.PhoneNumberFormatter.FormatUk(address.TelNo);

					if (!string.IsNullOrWhiteSpace(phoneNumber))
					{
						paymentRequest.CustomerPhone = phoneNumber;
						paymentRequest.CustomerWorkPhone = phoneNumber;
					}

					if (purchase.DeliveryAddressId == 0)
					{
						paymentRequest.ShippingDetails = new Opayo.Request.ShippingDetails(address);
						paymentRequest.ShippingDetails.RecipientFirstName = paymentRequest.CustomerFirstName;
						paymentRequest.ShippingDetails.RecipientLastName = paymentRequest.CustomerLastName;
					}
				}

				if (purchase.DeliveryAddressId != 0)
				{
					var address = await _addresses.Get(context, purchase.DeliveryAddressId, DataOptions.IgnorePermissions);
					if (address == null)
					{
						throw new PublicException("Could not locate delivery address.", "opaypo/missing_delivery_address");
					}

					paymentRequest.ShippingDetails = new Opayo.Request.ShippingDetails(address);
					paymentRequest.ShippingDetails.RecipientFirstName = paymentRequest.CustomerFirstName;
					paymentRequest.ShippingDetails.RecipientLastName = paymentRequest.CustomerLastName;

					var phoneNumber = Opayo.PhoneNumberFormatter.FormatUk(address.TelNo);

					if (!string.IsNullOrWhiteSpace(phoneNumber))
					{
						paymentRequest.CustomerPhone = phoneNumber;
					}

					if (purchase.BillingAddressId == 0)
					{
						paymentRequest.BillingAddress = new Opayo.Request.BillingAddress(address);
					}
				}

				if (_config.HostedPageEnabled)
				{
					// create a token to allow for purchase lookup from response from gateway
					var token = await _purchaseTokens.Create(context,
						new PurchaseToken()
						{
							PurchaseId = purchase.Id,
							Scope = "Payment",
							IsSingleUse = true,
							CreatedUtc = DateTime.UtcNow,
							ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
						}, DataOptions.IgnorePermissions);

					var encodedToken = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Token)));
					var pageCallbackUrl = AppSettings.GetPublicUrl(context.LocaleId) + "/cart/purchases/hosted/" + encodedToken;

					paymentRequest.SupportedPaymentMethods = new Dictionary<string, SupportedPaymentMethod>()
					{
						{ "card",
							new SupportedPaymentMethod()
							{
								Enabled = true,
								EnableSaveCard = true
							}
						}
					};

					var hostedPageRequest = new HostedPageRequest()
					{
						TransactionDetails = paymentRequest,
						CustomerDataCapture = new CustomerDataCapture()
						{
							CaptureAmount = false,
							CaptureBillingAddress = false,
							CaptureShippingAddress = false,
							CaptureFiData = false,
							CaptureEmail = false,
							CapturePhone = false
						},
						Presentation = new Presentation()
						{
							MerchantDomain = callbackUrl.Host,
							PaymentPageType = "redirect",
							ComponentVisibility = new ComponentVisibility()
							{
								DisplayAmount = true,
								DisplayCardLogos = true,
								DisplayDescription = true,
								DisplayLanguageSelector = false,
								DisplayVendorLogo = true,
								DisplayTerms = false
							},
							Language = new LanguageSelection()
							{
								PreselectedLanguage = "en",
								SupportedLanguageList = "en",
								SupportedLanguages = new Dictionary<string, SupportedLanguage>()
								{
									{ "en",
										new SupportedLanguage()
										{
											Enabled = true,
											Labels = new LanguageLabels()
											{
												CancelPay = "Cancel",
												Pay = "Pay"
											}
										}
									}
								}
							},
							ThemeCustomisation = new ThemeCustomisation()
							{
								PrimaryColour = "#b50e7d",
								SecondaryColour = "#b50e7d",
								SubmitColour = "#b50e7d"
							}
						},
						OutcomeReport = new OutcomeReport()
						{
							// once completed will redirct to page passing back transactionId and vendorTxCode as url parameters 
							RedirectUrls = new RedirectUrls()
							{
								CancelUrl = pageCallbackUrl,
								FailureUrl = pageCallbackUrl,
								ExpiryUrl = pageCallbackUrl,
								SuccessUrl = pageCallbackUrl
							},
							PostProcessNotification = new PostProcessNotification()
							{
								SendCustomerEmail = false,
								SendVendorEmail = false
							}
						}
					};

					var renderedPage = await _opayo.Post<Opayo.Response.RegisteredPageResponse>("/hosted-payment-pages/vendor/v1/payment-pages", hostedPageRequest);

					if (renderedPage == null)
					{
						throw new PublicException("Failed to process payment tranation.", "opaypo/invalid_hostedpage_transaction");
					}

					// Update purchase with Id from payment request (will get replaced later)
					purchase = await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
					{
						toUpdate.Status = string.IsNullOrWhiteSpace(renderedPage.NextURL) ? (uint)500 : (uint)103;
						toUpdate.PaymentGatewayInternalId = renderedPage.RegistrationId;
						toUpdate.GatewayPublicJson = new JsonString(JsonConvert.SerializeObject(_opayo.GetTransactionMessage(renderedPage)));
					});

					return new PurchaseAndAction()
					{
						Purchase = purchase,
						Action = renderedPage.NextURL
					};

				}

				var transationResponse = await _opayo.Post<Opayo.Response.TransactionResponse>("transactions", paymentRequest);

				if (transationResponse == null)
				{
					throw new PublicException("Failed to process payment tranation.", "opaypo/invalid_payment_transaction");
				}

				// Update purchase with Id from payment intent and the total cost:
				purchase = await _purchases.Update(context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) =>
				{
					// It might have instantly completed or instantly failed. We can find out from the status:
					toUpdate.Status = _opayo.ConvertStatus(transationResponse.Status);
					toUpdate.PaymentGatewayInternalId = transationResponse.TransactionId;
					toUpdate.GatewayResponseJson = new JsonString(_purchases.AppendResponseElement(orig.GatewayResponseJson.ValueOf(), transationResponse));
					toUpdate.GatewayPublicJson = new JsonString(JsonConvert.SerializeObject(_opayo.GetTransactionMessage(transationResponse)));
				});

				if (_config.VerboseLogging)
				{
					Log.Info("OpayoService", $"Opayo - Processed transation {purchase.Reference} {purchase.Status} {transationResponse.Status} {transationResponse.StatusDetail}");
				}

				var purchaseAction = new PurchaseAndAction()
				{
					Purchase = purchase
				};

				// requires additional challenge
				// or
				// saving card which forces 3ds
				if (purchase.Status == 300)
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
								IsSingleUse = true,
								CreatedUtc = DateTime.UtcNow,
								ExpiresUtc = DateTime.UtcNow.AddMinutes(15)
							}, DataOptions.IgnorePermissions);

						if (token == null)
						{
							throw new PublicException("Could not create session token.", "opaypo/missing_session_token");
						}

						purchaseAction.MetaData = new ChallengeMetaData()
						{
							// need to encode the token as it's also passed to the transation verification service
							SessionToken = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Token))),
							Token = token.Token,
							ChallengeRequest = transationResponse.CReq,
							ChallengeUrl = transationResponse.AcsUrl
						};

						return purchaseAction;
					}
				}

#if PAYMENTS_GUEST_USERS
				// if it's a guest pass back a token so that we can link to the order details 
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
						throw new PublicException("Could not create session token.", "opaypo/missing_session_token");
					}

					purchaseAction.MetaData = new ChallengeMetaData()
					{
						Token = token.Token
					};
				}
#endif

				return purchaseAction;
			}
		}


		/// <summary>
		/// Extract a user friendly status
		/// </summary>
		/// <param name="transationResponse"></param>
		/// <returns></returns>
		public List<string> GetTransactionMessage(Opayo.Response.TransactionResponse transationResponse)
		{
			// extract any user friendly status/errors
			var gatewayResponses = new List<string>();

			if (transationResponse.Errors != null)
			{
				foreach (var error in transationResponse.Errors)
				{
					gatewayResponses.Add(error.ToString());
				}
			}

			if (gatewayResponses.Count == 0)
			{
				gatewayResponses.Add(transationResponse.StatusDetail);
			}

			return gatewayResponses;
		}

		/// <summary>
		/// Extract a user friendly status
		/// </summary>
		/// <param name="transationResponse"></param>
		/// <returns></returns>
		public List<string> GetTransactionMessage(Opayo.Response.RegisteredPageResponse transationResponse)
		{
			// extract any user friendly status/errors
			var gatewayResponses = new List<string>();

			if (transationResponse.Errors != null)
			{
				foreach (var error in transationResponse.Errors)
				{
					gatewayResponses.Add(error.ToString());
				}
			}

			if (gatewayResponses.Count == 0)
			{
				gatewayResponses.Add(transationResponse.Status);
			}

			return gatewayResponses;
		}


		/// <summary>
		/// Make a get request to the opayo api
		/// </summary>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private async ValueTask<T> Get<T>(string endpoint, CancellationToken cancellationToken = default)
		{
			if (_config.VerboseLogging)
			{
				Log.Info(LogTag, $"DEBUG - Opayo Request : '{endpoint}'");
			}

			if (!Uri.TryCreate(_config.RestBaseURL, UriKind.Absolute, out _))
			{
				Log.Error(LogTag, $"Invalid base URL: '{_config.RestBaseURL}'");
				throw new PublicException("Payment gateway is not configured", "opaypo/missing_gateway_url");
			}

			string requestUrl = $"{_config.RestBaseURL.TrimEnd('/')}/{endpoint}";
			var requestUri = new Uri(requestUrl);

			var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

			var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.IntegrationKey}:{_config.IntegrationPassword}"));
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var response = await Http.SendAsync(request, cancellationToken);
			return await ReadOrThrow<T>(response);
		}


		/// <summary>
		/// Make a post request to the opayo api
		/// </summary>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private async ValueTask<T> Post<T>(string endpoint, object data, CancellationToken cancellationToken = default)
		{
			var json = JsonConvert.SerializeObject(data, _json);

			if (_config.VerboseLogging)
			{
				Log.Info(LogTag, $"DEBUG - Opayo Request : '{endpoint} {json}'");
			}

			if (!Uri.TryCreate(_config.RestBaseURL, UriKind.Absolute, out _))
			{
				Log.Error(LogTag, $"Invalid base URL: '{_config.RestBaseURL}'");
				throw new PublicException("Payment gateway is not configured", "opaypo/missing_gateway_url");
			}

			string requestUrl;
			if (endpoint.StartsWith('/'))
			{
				requestUrl = $"{_config.RestHost.TrimEnd('/')}{endpoint}";
			}
			else
			{
				requestUrl = $"{_config.RestBaseURL.TrimEnd('/')}/{endpoint}";
			}
			var requestUri = new Uri(requestUrl);

			var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};

			var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.IntegrationKey}:{_config.IntegrationPassword}"));
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var response = await Http.SendAsync(request, cancellationToken);
			return await ReadOrThrow<T>(response);
		}

		/// <summary>
		/// Read the response as <typeparamref name="T"/> 
		/// or 
		/// Throw exception
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="resp"></param>
		/// <returns></returns>
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
					Log.Error(LogTag, $"Invalid Opayo Response : '{resp.StatusCode} {text}'");
					throw new PublicException("No response from payment gateway :: " + resp.StatusCode.ToString(), "opaypo/invalid_response");
				}
				return ok;
			}

			if (resp.StatusCode == System.Net.HttpStatusCode.UnprocessableContent)
			{
				// The request was well-formed but contains invalid values or missing properties.
				var ok = JsonConvert.DeserializeObject<T>(text, _json);
				if (ok is null)
				{
					Log.Error(LogTag, $"Invalid Opayo Response : '{resp.StatusCode} {text}'");
					throw new PublicException("Invalid response from payment gateway :: " + resp.StatusCode.ToString(), "opaypo/unprocessable_response");
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
			if (errors != null && errors.Errors != null && errors.Errors.Count > 0)
			{
				foreach (var err in errors.Errors)
				{
					msg += err.ToString() + ", ";
				}

				error = errors.Errors[0];

			}
			else if (error != null)
			{
				msg = error.ToString();
			}

			Log.Error(LogTag, $"Invalid Opayo Response : '{msg} {(int)resp.StatusCode} {resp.ReasonPhrase}'");
			throw new PublicException($"Invalid response from payment gateway ({msg}) :: " + resp.StatusCode.ToString(), "opaypo/invalid_response");
		}
	}
}
