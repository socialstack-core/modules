using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;
using System.Reflection;
using Api.Startup;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Api.Currency
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CurrencyService : AutoService
    {
		private ExchangeRateService _exchangeRates;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CurrencyService(ExchangeRateService exchangeRates)
        {
			_exchangeRates = exchangeRates;

            var setupForTypeMethod = GetType().GetMethod(nameof(SetupForType));

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

                FieldInfo[] fields = servicedType.GetFields();
                List<FieldInfo> priceFields = new List<FieldInfo>();

                foreach (FieldInfo field in fields)
                {
                    var customAttributes = new List<Attribute>();

                    ContentField.BuildAttributes(field.CustomAttributes, customAttributes);
                    var priceAttribute = customAttributes.FirstOrDefault(ca => ca.GetType() == typeof(PriceAttribute));

                    if (priceAttribute != null)
                    {
                        priceFields.Add(field);
                    }
                }

                if (priceFields != null && priceFields.Count > 0)
                {
                    // Add Load and List events:
                    var setupType = setupForTypeMethod.MakeGenericMethod(new Type[] {
                        servicedType,
                        service.IdType
                    });

                    setupType.Invoke(this, new object[] {
                        service,
                        priceFields
                    });
                }
                
                return new ValueTask<AutoService>(service);
            });
        }

        /// <summary>
        /// Setup list and load events for entities that have price fields so that price can be calculated using exchange rates
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="ID"></typeparam>
        /// <param name="service"></param>
        /// <param name="priceFields"></param>
        public void SetupForType<T, ID>(AutoService<T, ID> service, List<FieldInfo> priceFields)
            where T : Content<ID>, new()
            where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
        {
            // If it's a mapping type, no-op.
            if (ContentTypes.IsAssignableToGenericType(typeof(T), typeof(Mapping<,>)))
            {
                return;
            }

            async ValueTask<T> ConvertPrices(Context ctx, T entity, List<FieldInfo> priceFields, List<ExchangeRate> exchangeRates)
            {
                foreach (FieldInfo field in priceFields)
                {
                    object value = field.GetValue(entity);

                    if (value == null)
                    {
                        foreach (var exchangeRate in exchangeRates)
                        {
                            var localisedEntity = await service.Get(new Context() { LocaleId = exchangeRate.FromLocaleId, DoNotConvertPrice = true }, entity.Id, DataOptions.IgnorePermissions);
                            var localisedValue = field.GetValue(localisedEntity);

                            if (localisedValue != null)
                            {
                                // Assumes price is a double, could cause some issues down the line
                                double? convertedValue = null;
                                try
                                {
                                    convertedValue = (double)localisedValue * exchangeRate.Rate;
                                }
                                catch (Exception e)
                                {
                                    // do nothing
                                }

                                if (convertedValue != null)
                                {
                                    field.SetValue(entity, convertedValue);
                                    // Currently uses first possible conversion we come across, might be nice to be able to specify prefered conversion(s) at some point
                                    break;
                                }
                            }
                        }
                    }
                }

                return entity;
            }

            async ValueTask<T> GetCurrencyLocalePrices(T entity, List<FieldInfo> priceFields, uint currencyLocaleId)
            {
                var currencyLocaleEntity = await service.Get(
                    new Context() { LocaleId = currencyLocaleId, DoNotConvertPrice = true }
                    ,entity.Id
                    ,DataOptions.IgnorePermissions 
                );

                if (currencyLocaleEntity != null)
                {
                    foreach (FieldInfo field in priceFields)
                    {
                        object currencyLocaleValue = field.GetValue(currencyLocaleEntity);

                        field.SetValue(entity, currencyLocaleValue);
                    }
                }

                return entity;
            }

            async ValueTask<T> ProcessEntity(Context ctx, T entity)
            {
                if (entity == null)
                {
                    return null;
                }

                var contextLocaleId = ctx.LocaleId;

                if (ctx.CurrencyLocaleId != 0 && ctx.CurrencyLocaleId != contextLocaleId)
                {
                    contextLocaleId = ctx.CurrencyLocaleId;
                    entity = await GetCurrencyLocalePrices(entity, priceFields, contextLocaleId);
                }

                var exchangeRates = await _exchangeRates.Where("ToLocaleId=?", DataOptions.IgnorePermissions).Bind(contextLocaleId).ListAll(ctx);

                if (exchangeRates != null && exchangeRates.Count > 0)
                {
                    entity = await ConvertPrices(ctx, entity, priceFields, exchangeRates);
                }

                return entity;
            }

            service.EventGroup.EndpointStartLoad.AddEventListener(async (Context ctx, ID id, HttpResponse response) =>
            {
                var request = response.HttpContext.Request;
                var referer = request.Headers.Referer;
                var isAdmin = referer.FirstOrDefault()?.Contains("en-admin");

                // Do not convert for admin pages
                if (isAdmin == true)
                {
                    ctx.DoNotConvertPrice = true;
                }

                return id;
            });

            service.EventGroup.EndpointStartList.AddEventListener(async (Context ctx, Filter<T, ID> filter, HttpResponse response) =>
            {
                var request = response.HttpContext.Request;
                var referer = request.Headers.Referer;
                var isAdmin = referer.FirstOrDefault()?.Contains("en-admin");

                // Do not convert for admin pages
                if (isAdmin == true)
                {
                    ctx.DoNotConvertPrice = true;
                }

                return filter;
            });
            
            service.EventGroup.AfterLoad.AddEventListener(async (Context ctx, T entity) =>
            {
                if (ctx.DoNotConvertPrice)
                {
                    return entity;
                }

                return await ProcessEntity(ctx, entity);
            });

            service.EventGroup.ListEntry.AddEventListener(async (Context ctx, T entity) =>
            {
                if (ctx.DoNotConvertPrice)
                {
                    return entity;
                }

                return await ProcessEntity(ctx, entity);
            });
        }
    }
    
}
