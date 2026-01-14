using Api.Contexts;
using Api.Startup;
using Api.Uploader;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Translate
{
    /// <summary>Handles translation endpoints.</summary>
    [Route("v1/translation")]
    public partial class TranslationController : AutoController<Translation>
    {
        private TranslationService _translationService;

        /// <summary>
        /// Instanced automatically.
        /// </summary>
        public TranslationController(
            TranslationService svc
        )
        {
            _translationService = svc;
        }


        /// <summary>
		/// Requests pre-populate the translations table 
		/// </summary>
		/// <returns></returns>
		[HttpGet("prepopulate")]
        public async ValueTask<object> PrePopulate(Context context)
        {
            // Admin and developer only:
            if (!context.Role.CanViewAdmin)
            {
                throw new PublicException("Admin only", "permissions", 403);
            }

            if (await _translationService.LoadDefaultTranslations())
            {
                return new
                {
                    success = true
                };
            }

            return null;
        }


        /// <summary>
        /// Process any pot files into the translations table 
        /// </summary>
        /// <returns></returns>
        [HttpGet("potfiles")]
        public async ValueTask<object> LoadPotFiles(Context context)
        {
            // Admin and developer only:
            if (!context.Role.CanViewAdmin)
            {
                throw new PublicException("Admin only", "permissions", 403);
            }

            if (await _translationService.LoadPotFiles(context))
            {
                return new
                {
                    success = true
                };
            }

            return null;
        }


    }
}