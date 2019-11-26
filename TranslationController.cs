using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Permissions;
using Api.Results;
using Api.AutoForms;
using Api.Contexts;
using Api.Eventing;

namespace Api.Translate
{
    /// <summary>
    /// Handles software endpoints.
    /// </summary>

    [Route("v1/translation")]
	[ApiController]
	public partial class TranslationController : ControllerBase
    {
        private IDatabaseService _database;
        private ITranslateService _translations;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public TranslationController(IDatabaseService database, ITranslateService translations)
        {
            _database = database;
            _translations = translations;
        }

		/// <summary>
		/// DELETE /v1/translation/2/
		/// Deletes a translation
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
        {
            // Permitted to do this?
            if (!Capabilities.TranslationDelete.IsGranted(Request, id))
            {
                Response.StatusCode = 403;
                return null;
            }
            
            bool deleted = await _translations.Delete(id);

            if (!deleted)
            {
                Response.StatusCode = 404;
                return null;
            }

            return new Success();
        }

		/// <summary>
		/// Handles uploading PO files.
		/// </summary>
		[HttpPost("po")]
		public async Task<object> Po([FromForm] PoUpload body)
        {
            // Permitted to do this?
            if (!Capabilities.TranslationPoUpload.IsGranted(Request))
            {
                Response.StatusCode = 403;
                return null;
            }

            var fileStream = body.File.OpenReadStream();
            
            // Parse the PO file, generating a translations set:
            var translations = await _translations.ParsePO(fileStream);

            if (translations == null)
            {
                // uh oh! Bad PO file.
                Response.StatusCode = 400;
                return new {
                    Type = "po/invalid"
                };
            }

            var localeId = translations.Locale;

            if (localeId == 0)
            {
                // uh oh! No locale defined in the po.
                Response.StatusCode = 400;
                return new {
                    Type = "po/no-locale"
                };
            }

            // Update now (but don't delete missing ones):
            var diff = await _translations.Update(translations, localeId, false);
            
            return new
            {
                added = diff.Added.Count,
                updated = diff.Changed.Count
            };

        }

		/// <summary>
		/// Updates the JSON file for a particular locale.
		/// </summary>
		/// <param name="localeId">ID of the locale.</param>
		/// <returns></returns>
		[HttpGet("rebuild/{localeId}")]
		public async Task<Success> Rebuild([FromRoute] int localeId)
        {
            // Generate the new JSON now:
            await _translations.UpdateJson(localeId);

            return new Success();
        }

		/// <summary>
		/// GET /v1/translation/locales/
		/// Search locales.
		/// </summary>
		[HttpPost("locales")]
		public async Task<Set<Locale>> Locales([FromBody] Search body)
        {
            // Permitted to do this?
            if (!Capabilities.TranslationLocaleSearch.IsGranted(Request))
            {
                Response.StatusCode = 403;
                return null;
            }

            var likeString = "%" + body.Query + "%";
			var results = await _translations.GetLocales();
            return new Set<Locale>(){ Results = results };
        }

		/// <summary>
		/// Generates a pot file for this sites text.
		/// </summary>
		/// <returns></returns>
        [HttpGet("pot")]
        public async Task<string> GetPot()
        {
            var pot = await _translations.GetPot();

            Response.Headers.Add("Content-Disposition", "attachment; filename=\"en_US.pot\"");

            return pot;
        }

		/// <summary>
		/// Download a PO for the given locale ID.
		/// </summary>
		[HttpGet("po/{localeId}")]
		public async Task<object> GetPo([FromRoute] int localeId)
        {
            var pot = await _translations.GetPo(localeId);

            if (string.IsNullOrWhiteSpace(pot))
            {
                // Didn't find anything for that locale.
                Response.StatusCode = 400;
                return new
                {
                    Type = "po/locale-not-found"
                };
            }

            Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + localeId + ".po\"");

            return pot;
        }

		/// <summary>
		/// Gets all the translations and info for a particular locale by its ID.
		/// </summary>
		[HttpGet("locale/{localeId}")]
		public async Task<object> Locale([FromRoute] int localeId)
        {

			// Get underlying locale info:
			var localeInfo = await _translations.GetLocaleEntry(localeId);

			if (localeInfo == null)
			{
				// Didn't find anything for that locale.
				Response.StatusCode = 400;
				return new
				{
					Type = "locale/locale-not-found"
				};
			}

			var translationSet = await _translations.GetLocale(localeId);
			
            return new {
                Locale = localeInfo,
                Translations = translationSet.All
            };
        }
		
		/// <summary>
		/// POST /v1/translation/
		/// Creates a new translation. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Translation> Create([FromBody] TranslationAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var translation = new Translation
			{
			};
			
			translation.ContentTypeId = ContentTypes.GetId(form.ContentType);
			
			if (!ModelState.Setup(form, translation))
			{
				return null;
			}

			form = await Events.TranslationCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			translation = await _translations.Create(context, form.Result);

			if (translation == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return translation;
        }
		
		/// <summary>
		/// POST /v1/translation/1/
		/// Updates a translation with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Translation> Update([FromRoute] int id, [FromBody] TranslationAutoForm form)
		{
			var context = Request.GetContext();

			var translation = await _translations.Get(context, id);
			
			if (!ModelState.Setup(form, translation)) {
				return null;
			}

			form = await Events.TranslationUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			translation = await _translations.Update(context, form.Result);

			if (translation == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return translation;
		}
		
    }
    
}
