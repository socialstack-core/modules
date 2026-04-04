using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.NavMenus;
using Api.Permissions;
using Api.Startup;
using Api.Startup.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>
	/// Conditions for a promotion placement.
	/// </summary>
	public partial class PlacementConditions
	{
		public List<uint> Pages { get; set; }
		public List<uint> SearchCategories { get; set; }
		public List<uint> Categories { get; set; }
		public bool IncludeChildren { get; set; } = true;
		public List<uint> Products { get; set; }
		public decimal? MinPrice { get; set; }
		public decimal? MaxPrice { get; set; }
	}

	/// <summary>
	/// A placement configuration from the JSON.
	/// </summary>
	public class PromoPlacementConfig
	{
		public string Type { get; set; }
		public PlacementConditions Conditions { get; set; }
	}

	/// <summary>
	/// A cache of promotions by placement type.
	/// String key is the placement type (e.g. "header", "search", "category", "product").
	/// </summary>
	public class PromotionCache : Dictionary<string, List<PromotionPlacement>> { }

	/// <summary>
	/// Handles promotions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PromotionService : AutoService<Promotion>
    {
	/// <summary>
	/// 
	/// </summary>
	private PromotionCache _promoCache;

	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public PromotionService() : base(Events.Promotion)
        {
			InstallAdminPages("Promotions", "fa:fa-percent", new[] { "id", "name", "isActive", "startDate", "endDate" }, null, "ecommerce");

			Events.Promotion.AfterUpdate.AddEventListener((Context context, Promotion promo, ChangedFields diff) =>
			{
				ClearCaches();
				return new ValueTask<Promotion>(promo);
			});

			Events.Promotion.AfterDelete.AddEventListener((Context context, Promotion promo) =>
			{
				ClearCaches();
				return new ValueTask<Promotion>(promo);
			});

			Events.Promotion.AfterCreate.AddEventListener((Context context, Promotion promo) =>
			{
				ClearCaches();
				return new ValueTask<Promotion>(promo);
			});

		}
		
		/// <summary>
		/// Gets the cached promo placements.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<PromotionCache> GetPlacements(Context context)
		{
			var cache = _promoCache;

			if (cache != null)
			{
				return cache;
			}

			cache = await BuildCache(context);
			return cache;
		}

		/// <summary>
		/// Gets cached promo placements filtered by placement type.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="placementType">The placement type to filter by (e.g. "header", "search"). Case-insensitive.</param>
		/// <returns></returns>
		public async ValueTask<List<PromotionPlacement>> GetPlacements(Context context, string placementType)
		{
			var cache = await GetPlacements(context);

			if (cache == null)
			{
				return null;
			}

			var typeKey = placementType?.ToLowerInvariant() ?? "";

			if (cache.TryGetValue(typeKey, out var placements))
			{
				return placements;
			}

			return null;
		}

		private async ValueTask<PromotionCache> BuildCache(Context context)
		{
			var allPromos = await Where("", DataOptions.IgnorePermissions).ListAll(context);

			var cache = new PromotionCache();

			foreach (var promo in allPromos)
			{
				var placementJson = promo.PlacementSitesJson;

				if (string.IsNullOrEmpty(placementJson))
				{
					continue;
				}

				List<PromoPlacementConfig> configs;

				try
				{
					configs = JsonConvert.DeserializeObject<List<PromoPlacementConfig>>(placementJson);
				}
				catch
				{
					continue;
				}

				if (configs == null || configs.Count == 0)
				{
					continue;
				}

				var now = DateTime.UtcNow;
				var isActivePeriod = promo.IsActive && 
					(!promo.EndDate.HasValue || promo.EndDate.Value >= now);

				foreach (var config in configs)
				{
					var placement = new PromotionPlacement
					{
						Promotion = promo,
						PlacementType = config.Type,
						Conditions = config.Conditions ?? new PlacementConditions(),
						IsValid = isActivePeriod
					};

					var typeKey = placement.PlacementType?.ToLowerInvariant() ?? "";

					if (!cache.TryGetValue(typeKey, out var list))
					{
						list = new List<PromotionPlacement>();
						cache[typeKey] = list;
					}

					list.Add(placement);
				}
			}

			_promoCache = cache;
			return cache;
		}

		private void ClearCaches()
		{
			// Need to update the two caches. We'll just wipe them for now:
			_promoCache = null;
			Router.RequestRebuild();
		}
	}

	/// <summary>
	/// A particular promo placement which can be quickly evaluated.
	/// </summary>
	public struct PromotionPlacement
	{
		/// <summary>
		/// The promo.
		/// </summary>
		public Promotion Promotion;

		/// <summary>
		/// The type of placement (e.g. "header", "search", "category", "product").
		/// </summary>
		public string PlacementType;

		/// <summary>
		/// The conditions for this placement.
		/// </summary>
		public PlacementConditions Conditions;

		/// <summary>
		/// Is this placement currently valid (active + within date range)?
		/// </summary>
		public bool IsValid;
	}

}
