using Api.Database;
using System.Threading.Tasks;
using Api.BlogPosts;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using Api.DatabaseDiff;
using System;

namespace Api.Translate
{
	/// <summary>
	/// Handles locales - the core of the translation (localisation) system.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class LocaleService : AutoService<Locale>, ILocaleService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LocaleService() : base(Events.Locale)
        {
			InstallAdminPages("Locales", "fa:fa-globe-europe", new string[] { "id", "name" });
		}
		
		private Locale[] _cache;
		
		
		/// <summary>
		/// Pops the cache.
		/// </summary>
		private async Task LoadCache()
		{
			// We create a new context because the cache is always Eng:
			var ctx = new Context();
			List<Locale> all = null;
			try
			{
				all = await List(ctx, null);
			} catch(Exception e)
			{
				// This isn't great - it needs to be something that any DBE would respond with, and use an error code.
				// However, for now, it'll successfully detect the error we want to make sure we're 
				// not creating more locales each time it happens to error during startup.
				if (e.Message.Contains("doesn't exist"))
				{

					// The table doesn't exist at all yet, and we're actively building the tables.
					// Set a default:
					var siteDefault = CreateDefault();

					_cache = new Locale[1] {
						siteDefault
					};

					// And also handle the post table creation event to then insert the default:
					Events.DatabaseDiffAfterAdd.AddEventListener(async (Context context, DiffSet<DatabaseTableDefinition, DiffSet<DatabaseColumnDefinition, ChangedColumn>> ds) =>
					{
						// Create the default now, using the definite locale #1 ctx:
						await Create(ctx, siteDefault);

						return ds;
					});

				}
				return;
			}

			// Cache is indexed by locale ID.
			
			var maxId = 0;
			foreach(var locale in all){
				if(locale.Id > maxId){
					maxId = locale.Id;
				}
			}

			if (maxId == 0)
			{
				// None - create the site default one now, with a forced ID.
				var siteDefault = CreateDefault();
				
				var cacheSet = new Locale[1] {
					siteDefault
				};

				_cache = cacheSet;
				
				// Create with an explicit locale, because name is itself localised
				// and we want to ensure its set to the correct field.
				await Create(ctx, siteDefault);

				// Create clears the cache so we just set it again here:
				_cache = cacheSet;

				all = new List<Locale>() {
					siteDefault
				};

				return;
			}
			else
			{
				// Create the cache:
				_cache = new Locale[maxId];
			}

			if (maxId > 5000){
				// There aren't this many locales; a database tidy reccommended.
				System.Console.WriteLine("Warning: Your locales have inefficiently high IDs. Consider tidying your locales by assigning them new IDs.");
			}
			
			// Populate it:
			foreach(var locale in all){
				_cache[locale.Id - 1] = locale;
			}

			if (_cache[0] == null)
			{
				_cache[0] = CreateDefault();
				System.Console.WriteLine("Warning: Locale with ID 1 must exist, and be EN.");
			}
		}

		/// <summary>
		/// Returns an object which is a suitable site default locale.
		/// </summary>
		/// <returns></returns>
		private Locale CreateDefault()
		{
			return new Locale()
			{
				Code = "en",
				Name = "English",
				Id = 1
			};
		}

		/// <summary>
		/// Updates the database with the given locale data. It must have an ID set.
		/// </summary>
		public override Task<Locale> Update(Context context, Locale locale)
		{
			_cache = null;
			return base.Update(context, locale);
		}
		
		/// <summary>
		/// Create a new locale.
		/// </summary>
		public override Task<Locale> Create(Context context, Locale locale)
		{
			_cache = null;
			return base.Create(context, locale);
		}
		
		/// <summary>
		/// Delete a locale by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public override Task<bool> Delete(Context context, int id)
		{
			_cache = null;
			return base.Delete(context, id);
		}

		/// <summary>
		/// Gets the locale cache. The locale Names are always English for now, but will be localised itself in the future.
		/// </summary>
		public async Task<Locale[]> GetAllCached(Context context)
		{
			if (_cache == null)
			{
				await LoadCache();
			}

			return _cache;
		}

		/// <summary>
		/// Gets a locale from the cache. The locale Name is always English for now, but will be localised itself in the future.
		/// </summary>
		public async Task<Locale> GetCached(Context context, int id)
		{
			if(_cache == null){
				await LoadCache();
			}
			
			if(id < 0 || id>=_cache.Length){
				return null;
			}
			
			return _cache[id-1];
		}
		
	}
    
}
