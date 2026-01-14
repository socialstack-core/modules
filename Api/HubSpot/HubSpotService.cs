using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System;
using Api.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http.Headers;
using Api.Users;
using Api.Startup;
using Api.Automations;
using Api.Eventing;
using Api.Database;

namespace Api.HubSpot
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HubSpotService : AutoService
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		/// 

		private UserService _userService;
		private HubSpotConfig _config;
		private readonly static Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => new HttpClient());
		private readonly JsonSerializerSettings _json;

		private HttpClient Http { get => _httpClient.Value; }

		/// <summary>
		/// Service to retrieve and push contact data into hubspot
		/// </summary>
		public HubSpotService(UserService userService)
		{
			_userService = userService;

			_config = GetConfig<HubSpotConfig>();
			_json = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				MissingMemberHandling = MissingMemberHandling.Ignore,
				DateParseHandling = DateParseHandling.DateTimeOffset,
			};

			// Check for new accounts which need to be sync'd to hubspot
			Events.Automation("HubSpot Add New Contacts", "0 0 * ? * * *", false, "Add any new contacts to hubspot").AddEventListener(async (Context context, AutomationRunInfo info) =>
			{
				await ProcessNewContacts(context);

				return info;
			});
		}

		/// <summary>
		/// Add any new contacts into hubspot
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		public async ValueTask ProcessNewContacts(Context ctx)
		{
			if (string.IsNullOrWhiteSpace(_config.ApiKey) || _config.EnableSync == false)
			{
				return;
			}

			foreach (var kvp in Services.All)
			{
				// Get the content type for this service and event group:
				var servicedType = kvp.Value.ServicedType;
				if (servicedType == null)
				{
					continue;
				}

				if (!typeof(IHaveHubSpotId).IsAssignableFrom(servicedType))
				{
					continue;
				}

				if (_config.Mappings == null || !_config.Mappings.Any(e => e.Entity == servicedType.Name))
				{
					continue;
				}

				// setup the generic method on this class which will basically accept any AutoService.
				var getItemstoProcessMethod = GetType().GetMethod(nameof(GetPendingList));

				// Create a specific flavour of that method:
				var setupItemstoProcessMethod = getItemstoProcessMethod.MakeGenericMethod(new Type[] {
						kvp.Value.ServicedType,
						kvp.Value.IdType
				});

				// Invoke it asynchronously and wait
				await (Task)setupItemstoProcessMethod.Invoke(this, new object[] { kvp.Value, ctx });
			}
		}

		/// <summary>
		/// Search contacts with one or more filters.
		/// </summary>
		/// <param name="filters"></param>
		/// <param name="limit"></param>
		/// <param name="type"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		public async Task<HubSpotListResponse<HubSpotEntity>> Search(IEnumerable<(string property, string op, string value)> filters, int limit = 10, string type = "contacts", IEnumerable<string> properties = null)
		{
			var reqBody = new SearchRequest
			{
				FilterGroups = new()
				{
					new FilterGroup
					{
						Filters = filters.Select(f => new Filter
						{
							PropertyName = f.property,
							Operator     = f.op,
							Value        = f.value
						}).ToList()
					}
				},
				Limit = limit,
				Properties = properties?.ToList()
			};

			if (!Uri.TryCreate(_config.Url, UriKind.Absolute, out _))
			{
				Log.Warn(LogTag, $"Invalid base URL: '{_config.Url}'");
				return default;
			}

			string requestUrl = $"{_config.Url.TrimEnd('/')}/objects/{type}/search";
			var requestUri = new Uri(requestUrl);

			var json = JsonConvert.SerializeObject(reqBody, _json);
			var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};

			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var response = await Http.SendAsync(request);
			return await ReadOrThrow<HubSpotListResponse<HubSpotEntity>>(response);
		}

		/// <summary>
		/// List contacts with optional properties and paging.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="limit"></param>
		/// <param name="after"></param>
		/// <param name="properties"></param>
		/// <param name="archived"></param>
		/// <returns></returns>

		public async Task<HubSpotListResponse<HubSpotEntity>> List(string type = "contacts", int limit = 100, string after = null, IEnumerable<string> properties = null, bool archived = false)
		{
			var qs = new List<string> { $"limit={limit}", $"archived={(archived ? "true" : "false")}" };

			if (!string.IsNullOrWhiteSpace(after))
			{
				qs.Add($"after={Uri.EscapeDataString(after)}");
			}

			if (properties is not null)
			{
				qs.AddRange(properties.Select(p => $"properties={Uri.EscapeDataString(p)}"));
			}

			if (!Uri.TryCreate(_config.Url, UriKind.Absolute, out _))
			{
				Log.Warn(LogTag, $"Invalid base URL: '{_config.Url}'");
				return default;
			}

			string requestUrl = $"{_config.Url.TrimEnd('/')}/objects/{type}?{string.Join("&", qs)}";
			var requestUri = new Uri(requestUrl);

			var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var resp = await Http.SendAsync(request);
			return await ReadOrThrow<HubSpotListResponse<HubSpotEntity>>(resp);
		}

		/// <summary>
		/// Create an entity in hubspot with arbitrary properties.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="type"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		public async Task<T> Create<T>(string type, IDictionary<string, object> properties)
		{
			// wrap the incoming properties inside an object with a single "properties" field
			var payload = new Dictionary<string, object> { ["properties"] = properties };
			var json = JsonConvert.SerializeObject(payload, _json);

			if (!Uri.TryCreate(_config.Url, UriKind.Absolute, out _))
			{
				Log.Warn(LogTag, $"Invalid base URL: '{_config.Url}'");
				return default;
			}

			string requestUrl = $"{_config.Url.TrimEnd('/')}/objects/{type}";
			var requestUri = new Uri(requestUrl);

			var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};

			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var response = await Http.SendAsync(request);
			return await ReadOrThrow<T>(response);
		}

		/// <summary>
		/// Ensure a contact exists: if found by email, return it; otherwise create it.
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="entity"></param>
		/// <param name="id"></param>
		/// <param name="key"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public async Task<(HubSpotEntity entity, bool created)> CreateEntity<T, ID>(Context ctx, T entity, string id, string key = "email", string type = "contacts")
			where T : Content<ID>, IHaveHubSpotId, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			if (entity == null)
			{
				return (null, created: false);
			}

			if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(id))
			{
				// check for existing
				var found = await Search(
					new[] { (property: key, op: "EQ", value: id) },
					limit: 1,
					properties: null,
					type: type
				);

				// if exists then return the entity
				if (found != null && found.Results.Count > 0)
				{
					await UpdateEntity<T, ID>(ctx, entity, found.Results[0]);

					return (found.Results[0], created: false);
				}
			}

			var properties = await MapEntity<T, ID>(ctx, entity);

			if (properties == null || properties.Count == 0)
			{
				return (null, created: false);
			}

			var hubSpotEntity = await Create<HubSpotEntity>(type, properties);

			if (hubSpotEntity != null && !string.IsNullOrWhiteSpace(hubSpotEntity.Id))
			{
				await UpdateEntity<T, ID>(ctx, entity, hubSpotEntity);
			}

			return (hubSpotEntity, created: hubSpotEntity != null);
		}

		/// <summary>
		/// Read the response as <typeparamref name="T"/> 
		/// or 
		/// throw a <see cref="HubSpotApiException"/> with parsed problem details.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="resp"></param>
		/// <returns></returns>
		/// <exception cref="HubSpotApiException"></exception>
		private async Task<T> ReadOrThrow<T>(HttpResponseMessage resp)
		{
			var text = await resp.Content.ReadAsStringAsync();

			if (resp.IsSuccessStatusCode)
			{
				var ok = JsonConvert.DeserializeObject<T>(text, _json);
				if (ok is null)
				{
					throw new HubSpotApiException(resp.StatusCode, "Empty/invalid JSON payload.");
				}
				return ok;
			}

			HubSpotProblem problem = null;
			try
			{
				problem = JsonConvert.DeserializeObject<HubSpotProblem>(text, _json);
			}
			catch
			{
				/* ignore */

			}

			var corrId = resp.Headers.TryGetValues("X-HubSpot-Correlation-Id", out var vals) ? vals.FirstOrDefault() : null;

			var msg = problem?.ToString() ?? $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {text}";
			throw new HubSpotApiException(resp.StatusCode, msg, problem, corrId);
		}

		/// <summary>
		/// Retrieve a list of pending Update the entity with hubspot id 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="service"></param>
		/// <param name="ctx"></param>
		/// <returns></returns>
		public async Task GetPendingList<T, ID>(AutoService<T, ID> service, Context ctx)
			where T : Content<ID>, IHaveHubSpotId, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var pending = await service
				.Where("SyncToHubSpot=?", DataOptions.IgnorePermissions)
				.Bind(true)
				.ListAll(ctx);

			if (pending == null || pending.Count == 0)
			{
				return;
			}

			var fieldInfo = await service.GetJsonStructure(ctx);

			var emailField = fieldInfo.AllFields
				.FirstOrDefault(s => s.Key.Equals("email", StringComparison.OrdinalIgnoreCase))
				.Value;

			if (emailField == null)
			{
				return;
			}

			foreach (var entity in pending)
			{
				try
				{
					var email = emailField.FieldInfo.GetValue(entity) as string;

					if (string.IsNullOrWhiteSpace(email))
					{
						continue;
					}

					var result = await CreateEntity<T, ID>(ctx, entity, email, "email", "contacts");
					if (result.entity != null && !string.IsNullOrWhiteSpace(result.entity.Id))
					{
						if (result.created)
						{
							Log.Info(LogTag, $"Added user {entity.Id} to hubspot");
						}
						else
						{
							Log.Info(LogTag, $"User {entity.Id} already exists in hubspot");
						}
					}
					else
					{
						Log.Warn(LogTag, $"Failed to add user {entity.Id} to hubspot, check email address");
					}
				}
				catch (Exception e)
				{
					// failed to update 
					Log.Error(LogTag, e, $"Failed to add user {entity.Id} to hubspot");
				}
			}
		}


		/// <summary>
		/// Update the entity with hubspot id 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="ctx"></param>
		/// <param name="entity"></param>
		/// <param name="hubspotEntity"></param>
		/// <returns></returns>
		private async ValueTask<T> UpdateEntity<T, ID>(Context ctx, T entity, HubSpotEntity hubspotEntity)
			where T : Content<ID>, IHaveHubSpotId, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var entityType = typeof(T);
			var entityService = Services.GetByContentType(entityType);

			var updated = await (entityService as AutoService<T, ID>).Update(ctx, entity, (ctx, toUpdate, orig) =>
			{
				toUpdate.SetHubSpotId(hubspotEntity.Id);
				toUpdate.SetHubSpotSync(null);
			}, DataOptions.IgnorePermissions);

			return updated;
		}


		/// <summary>
		/// Map a ss entity into a hubspot set of properties
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="entity"></param>
		/// <returns></returns>
		private async ValueTask<IDictionary<string, object>> MapEntity<T, ID>(Context ctx, T entity)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var entityType = typeof(T);

			if (_config.Mappings == null || !_config.Mappings.Any(e => e.Entity == entityType.Name))
			{
				return null;
			}

			var targetSvc = Services.GetByContentType(entityType);
			if (targetSvc == null)
			{
				return null;
			}

			var hubspotProperties = new Dictionary<string, object>();

			var mappings = _config.Mappings.FirstOrDefault(e => e.Entity == entityType.Name).FieldNames;
			if (mappings != null && mappings.Count > 0)
			{
				// process any mappings from PO fields 
				var fieldInfo = await targetSvc.GetJsonStructure(ctx);
				foreach (var fieldPair in fieldInfo.AllFields)
				{
					if (mappings.TryGetValue(fieldPair.Value.Name, out var fieldName))
					{
						if (fieldPair.Value != null && fieldPair.Value.FieldInfo != null && !string.IsNullOrWhiteSpace(fieldPair.Value.FieldInfo.GetValue(entity)?.ToString()))
						{
							hubspotProperties.TryAdd(fieldName, fieldPair.Value.FieldInfo.GetValue(entity));
						}
					}
				}
			}

			// now check if there are any additional handlers to add extra values
			if ((targetSvc as AutoService<T, ID>).EventGroup.HubSpotProperties.HasListeners())
			{
				hubspotProperties = await (targetSvc as AutoService<T, ID>).EventGroup.HubSpotProperties.Dispatch(ctx, hubspotProperties, entity);
			}

			// finally add any defaults 
			if (_config.DefaultMappings != null && _config.DefaultMappings.Any())
			{
				foreach (var defaultMapping in _config.DefaultMappings)
				{
					if (hubspotProperties.ContainsKey(defaultMapping.Key))
					{
						continue;
					}

					hubspotProperties.TryAdd(defaultMapping.Key, defaultMapping.Value);
				}
			}

			return hubspotProperties;
		}
	}
}
