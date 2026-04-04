using Api.Database;
using Api.Eventing;
using Api.Startup;
using Api.Translate;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Context = Api.Contexts.Context;

namespace Api.SearchMetaData
{

    /// <summary>
    /// Add additional meta data to the document indexing
    /// </summary>

    public partial class SearchMetaDataService : AutoService
    {
        private SearchMetaDataConfig _cfg;

        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public SearchMetaDataService()
        {
            _cfg = GetConfig<SearchMetaDataConfig>();

            // subscribe to events triggered by content types so we can add index listeners 
            Events.Service.AfterCreate.AddEventListener((Context ctx, AutoService service) =>
            {
                if (service == null)
                {
                    return new ValueTask<AutoService>(service);
                }

                // Get the content type for this service and event group:
                var servicedType = service.ServicedType;
                if (servicedType == null)
                {
					// Things like the ffmpeg service.
					return new ValueTask<AutoService>(service);
				}

                if (_cfg == null || _cfg.Mappings == null)
                {
					return new ValueTask<AutoService>(service);
				}

                // ensure we are configued for the entity
                var mapping = _cfg.Mappings.FirstOrDefault(i => i.Name == service.EntityName);
                if (mapping == null)
                {
					return new ValueTask<AutoService>(service);
				}

                if (mapping.FieldNames == null || mapping.FieldNames.Count == 0)
                {
                    Log.Warn(LogTag, $"{service.EntityName}Service : Incorrect config (fieldnames)");
					return new ValueTask<AutoService>(service);
				}

                var setupForTypeMethod = GetType().GetMethod(nameof(SetupForType));

                // Add List event:
                var setupType = setupForTypeMethod.MakeGenericMethod(new Type[] {
                    servicedType,
                    service.IdType
                });

                setupType.Invoke(this, new object[] {
                    service
                });
				return new ValueTask<AutoService>(service);
			});

        }

        /// <summary>
        /// Handler for content types to allow for custom meta data to be extracted
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="ID"></typeparam>
        /// <param name="service"></param>
        public void SetupForType<T, ID>(AutoService<T, ID> service)
            where T : Content<ID>, new()
            where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
        {

            if (_cfg.DebugToConsole)
            {
                Log.Info(LogTag, $"{service.EntityName}Service : Registering Search Metadata");
            }

            // Content types that are configured for additional metaddata indexing processed here.    
            service.EventGroup.SearchMetaData.AddEventListener(async (Context ctx2, HashSet<string> metadataText, T po) =>
            {
                if (_cfg == null || _cfg.Mappings == null || po == null)
                {
                    return metadataText;
                }

				var anonContext = new Context();

				if (metadataText == null)
                {
                    metadataText = new HashSet<string>();
                }

                // ensure we are configued for the entity (incase has changed since handler was created)
                var mapping = _cfg.Mappings.FirstOrDefault(i => i.Name == service.EntityName);
                if (mapping == null || mapping.FieldNames == null || mapping.FieldNames.Count == 0)
                {
                    return metadataText;
                }

                // inject the primary object into the metadata 
                metadataText.UnionWith(ExtractMetaDataStrings(anonContext, mapping.FieldNames, po));

                if (mapping.Includes != null && mapping.Includes.Count > 0)
                {
                    var uniqueIncludeNames = mapping.Includes
                        .Where(i => 
                            !string.IsNullOrWhiteSpace(i.Name) &&
                            i.FieldNames != null && 
                            i.FieldNames.Count > 0
                        )
                        .Select(i => i.Name)
                        .Distinct();

                    // get the includes (as per front end calls we get json back initially)
                    var jsonString = await service.ToJson(anonContext, po, string.Join(",", uniqueIncludeNames));
                    var searchMetaData = JsonConvert.DeserializeObject<SearchMetaData>("{\"primaryObject\" : " + jsonString + "}");

                    dynamic includedService = null;
                    string includeType = "";

                    // map the includes back to real objects
                    foreach (var include in searchMetaData.Includes)
                    {
                        foreach (var entity in include.Values)
                        {
                            string typeName = entity["type"]?.ToString();
                            string id = entity["id"]?.ToString();

                            if (!string.IsNullOrWhiteSpace(typeName) && !string.IsNullOrWhiteSpace(id))
                            {
								var fieldNames = mapping.Includes.FirstOrDefault(i => i.Name.Equals(include.Name, StringComparison.InvariantCultureIgnoreCase))?.FieldNames;

								if (fieldNames == null || fieldNames.Count() == 0)
								{
									continue;
								}

								// if multiple depth include, find the correct fields and extract the field names
								/*
								{
									"Name" : "productQuantities.product",
									"FieldNames" : [
										"product.name",
										"product.sku",
										"productQuantities.quantity"
									]
								}
								or for simple mappings
								{
									"Name" : "attributes",
									"FieldNames" : [
										"Value"
									]
								}
								*/

								if (include.Name.Contains('.'))
								{
									fieldNames = fieldNames.
									Where(a => a.StartsWith(include.Field + ".", StringComparison.InvariantCultureIgnoreCase))
									.Select(a => a.Split('.').Last())
									.ToList();

									if (fieldNames == null || fieldNames.Count() == 0)
									{
										continue;
									}
								}

								if (typeName != includeType)
                                {
                                    // get the relevant service
                                    includeType = typeName;
                                    includedService = Services.Get($"{includeType}Service");
                                }

                                // map into real object so that use field definitions etc 
                                var dataItem = await includedService.Where("Id=?", DataOptions.IgnorePermissions).Bind(Convert.ChangeType(id, TypeCode.UInt32)).First(anonContext);
                                if (dataItem != null)
                                {
									metadataText.UnionWith(ExtractMetaDataStrings(anonContext, fieldNames, dataItem));
                                }
                            }
                        }
                    }
                }

                return metadataText;
            }, 5);

            service.EventGroup.BeforeCreate.AddEventListener(async (ctx, entity) =>
            {
                return await HandleSearchMetaData(ctx, entity, service);
            }, 99);

            service.EventGroup.BeforeUpdate.AddEventListener(async (Context ctx, T updated, T orig) =>
            {
                return await HandleSearchMetaData(ctx, updated, service);
            }, 99);
        }

        private async Task<T> HandleSearchMetaData<T, ID>(Context ctx2, T entity, AutoService<T, ID> service)
            where T : Content<ID>, new()
            where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
        {
            if (!service.EventGroup.SearchMetaData.HasListeners())
            {
                return entity;
            }

			var anonContext = new Context();

			var fieldInfo = await service.GetJsonStructure(anonContext);
            var metadataField = fieldInfo.AllFields
                .FirstOrDefault(s => s.Key.Equals("descriptionraw", StringComparison.OrdinalIgnoreCase))
                .Value;

            if (metadataField != null)
            {
                var metaData = new HashSet<string>();
                metaData = await service.EventGroup.SearchMetaData.Dispatch(anonContext, metaData, entity);
                metadataField.FieldInfo.SetValue(entity, string.Join(" ", metaData));
            }

            return entity;
        }

        /// <summary>
        /// Extract any strings from an object
        /// used to extract indexable content from primaryObjects and metadata (includes)
        /// also parses and updates the values in the passed in object 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fieldNames"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private IEnumerable<string> ExtractMetaDataStrings<TEntity>(Context ctx, List<string> fieldNames, TEntity entity)
        {
            var stringValues = new HashSet<string>();

            if (fieldNames== null || fieldNames.Count==0)
            {
                return stringValues;
            }

            foreach (var field in entity.GetType().GetFields())
            {
                // ONLY include configured fields
                if (!fieldNames.Contains(field.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                string value = null;

                if (field.FieldType == typeof(Localized<JsonString>))
                {
                    value = ((Localized<JsonString>)field.GetValue(entity)).Get(ctx).ValueOf();
                }
                else if (field.FieldType == typeof(Localized<string>))
                {
                    value = ((Localized<string>)field.GetValue(entity)).Get(ctx);
					value = CheckForHtml(value);
				}
				else if (field.FieldType == typeof(uint))
				{
					var fieldValue = field.GetValue(entity) as uint?;
					if (fieldValue.GetValueOrDefault(0) != 0)
					{
						value = fieldValue == null ? null : fieldValue.ToString();
					}
				}
				else
				{
                    var fieldValue = field.GetValue(entity);
					value = fieldValue == null ? null : fieldValue.ToString();
                }

                // ignore empty strings, links or images
                if (string.IsNullOrWhiteSpace(value) ||
                    value.StartsWith("public:") ||
                    value.StartsWith("private:") ||
                    value.StartsWith("http://") ||
                    value.StartsWith("https://"))
                {
                    continue;
                }

                if (field.FieldType == typeof(Localized<JsonString>))
                {
                    var plainTextItems = GetPlainText(value);

                    if (plainTextItems != null)
                    {
                        stringValues.UnionWith(plainTextItems);
                    }
                }
				else
				{
                    if (_cfg.DebugToConsole)
                    {
                        Log.Info("Search Metadata", $"Plain text value - {entity.GetType().Name} {field.Name} {value}");
                    }

                    stringValues.Add(value);
                }
            }

            return stringValues;
        }

        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.None
        };

        private static HashSet<string> GetPlainText(string content)
        {
            try
            {
                JToken root = JToken.Parse(content);
                return ExtractTextValues(root);
            }
            catch (Exception ex)
            {
                Log.Error("Search Indexing", $"Failed to extract plain text [{content}] [{ex.ToString}]");
            }

            return null;
        }

        /// <summary>
        /// Extract any string/text entries from a canvas element
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static HashSet<string> ExtractTextValues(JToken token)
        {
            var result = new HashSet<string>();

            void Traverse(JToken current)
            {
                if (current.Type == JTokenType.Object)
                {
                    foreach (var prop in current.Children<JProperty>())
                    {
                        if (prop.Name == "s" && prop.Value.Type == JTokenType.String)
                        {
                            result.Add(prop.Value.ToString());
                        }
                        else
                        {
                            Traverse(prop.Value);
                        }
                    }
                }
                else if (current.Type == JTokenType.Array)
                {
                    foreach (var item in current.Children())
                    {
                        Traverse(item);
                    }
                }
            }

            Traverse(token);
            return result;
        }

		/// <summary>
		/// Is the string html or plain text
		/// </summary>
		/// <param name="raw"></param>
		/// <returns></returns>
		private static string CheckForHtml(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return raw;
			}

			var doc = new HtmlDocument();
			doc.LoadHtml(raw);

			// If the parsed document has any elements (not just text), it’s HTML
			if (doc.DocumentNode.Descendants().Any(n => n.NodeType == HtmlNodeType.Element))
			{
				return ExtractPlainText(doc);
			}

			return raw;
		}

		/// <summary>
		/// Extract plain text from htmldocument
		/// </summary>
		/// <param name="doc"></param>
		/// <returns></returns>
		private static string ExtractPlainText(HtmlDocument doc)
		{
			if (doc == null)
			{
				return string.Empty;
			}

			// Drop non-visible sources
			foreach (var n in doc.DocumentNode.SelectNodes("//script|//style|//noscript|//template") ?? Enumerable.Empty<HtmlNode>())
			{
				n.Remove();
			}

			//Get text and decode entities once
			var text = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);

			// Normalize whitespace
			text = Regex.Replace(text, @"\s+", " ");

			return text.Trim();
		}
	}
}

