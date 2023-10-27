using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using System.IO;
using Microsoft.Extensions.Primitives;
using Api.Startup;
using System.Collections.Generic;
using System.Linq;
using Api.Permissions;
using System.Web;
using Api.Automations;
using Api.Eventing;
using Api.Translate;
using System;

namespace Api.Uploader
{
    /// <summary>
    /// Handles file upload endpoints.
    /// </summary>

    [Route("v1/upload")]
    public partial class UploadController : AutoController<Upload>
    {
        /// <summary>
        /// Instanced automatically.
        /// </summary>
        public UploadController() : base()
        {
        }

        /// <summary>
        /// Upload a file.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpPost("create")]
        public async ValueTask Upload([FromForm] FileUploadBody body)
        {
            var context = await Request.GetContext();

            // body = await Events.Upload.Create.Dispatch(context, body, Response) as FileUploadBody;

            var fileName = body.File.FileName;

            if (string.IsNullOrEmpty(fileName) || fileName.IndexOf('.') == -1)
            {
                throw new PublicException("Content-Name header should be a filename with the type.", "invalid_name");
            }

            // Write to a temporary path first:
            var tempFile = System.IO.Path.GetTempFileName();

            // Save the content now:
            var fileStream = new FileStream(tempFile, FileMode.OpenOrCreate);

            await body.File.CopyToAsync(fileStream);
            fileStream.Close();

            // Upload the file:
            var upload = await (_service as UploadService).Create(
                context,
                fileName,
                tempFile,
                null,
                body.IsPrivate
            );

            if (upload == null)
            {
                // It failed. Usually because white/blacklisted.
                Response.StatusCode = 401;
                return;
            }

            await OutputJson(context, upload, "*");
        }

        /// <summary>
        /// Upload a file with efficient support for huge ones.
        /// </summary>
        /// <returns></returns>
        [HttpPut("create")]
        public async ValueTask Upload()
        {
            if (!Request.Headers.TryGetValue("Content-Name", out StringValues name))
            {
                throw new PublicException("Content-Name header is required", "no_name");
            }

            var fileName = HttpUtility.UrlDecode(name.ToString());

            if (string.IsNullOrEmpty(fileName) || fileName.IndexOf('.') == -1)
            {
                throw new PublicException("Content-Name header should be a filename with the type.", "invalid_name");
            }

            var isPrivate = false;

            if (Request.Headers.TryGetValue("Private-Upload", out StringValues privateState))
            {
                var privState = privateState.ToString().ToLower().Trim();

                isPrivate = privState == "true" || privState == "1" || privState == "yes";
            }

            var context = await Request.GetContext();

            // The stream for the actual file is just the entire body:
            var contentStream = Request.Body;

            var tempFile = System.IO.Path.GetTempFileName();

            var fileStream = new FileStream(tempFile, System.IO.FileMode.OpenOrCreate);
            await contentStream.CopyToAsync(fileStream);
            fileStream.Close();

            // Create the upload entry for it:
            var upload = await (_service as UploadService).Create(
                context,
                fileName,
                tempFile,
                null,
                isPrivate
            );

            if (upload == null)
            {
                // It failed for some generic reason.
                Response.StatusCode = 401;
                return;
            }

            await OutputJson(context, upload, "*");
        }

        /// <summary>
        /// Uploads a transcoded file. The body of the client request is expected to be a tar of the files, using a directory called "output" at its root.
        /// </summary>
        /// <returns></returns>
        [HttpPut("transcoded/{id}")]
        public async ValueTask TranscodedTar([FromRoute] uint id, [FromQuery] string token)
        {
            if (!(_service as UploadService).IsValidTranscodeToken(id, token))
            {
                throw new PublicException("Invalid transcode token", "tx_token_bad");
            }

            var context = await Request.GetContext();

            // Proceed only if the target doesn't already exist.
            // Then there must be a GET arg called sig containing an alphachar HMAC of recent time-id. It expires in 24h.

            // The stream for the actual file is just the entire body:
            var contentStream = Request.Body;

            // Expect a tar:
            await (_service as UploadService).ExtractTarToStorage(context, id, "chunks", contentStream);
        }

        /// <summary>
        /// List any active media items
        /// </summary>
        [HttpGet("active")]
        public async ValueTask Active([FromQuery] string includes)
        {
            var context = await Request.GetContext();

            var usageMap = new Dictionary<uint, int>();
            List<Upload> uploads = new List<Upload>();

            // Get all the current locales:
            var _locales = Services.Get<LocaleService>();
            var locales = await _locales.Where("").ListAll(context);

            foreach (var locale in locales)
            {
                context.LocaleId = locale.Id;

                // loop through all content and look for any media refs in use 
                foreach (var kvp in Services.All)
                {
                    var activeRefs = await kvp.Value.ActiveRefs(context, usageMap);
                    if (activeRefs != null && activeRefs.Any())
                    {
                        uploads.AddRange(activeRefs);
                    }
                }
            }
            await OutputJson(context, uploads.OrderBy(u => u.OriginalName).ToList(), includes, true);
        }

        /// <summary>
        /// List any active media refs
        /// </summary>
        [HttpPost("active")]
        public async ValueTask ActivePost([FromQuery] string includes)
        {
            await Active(includes);
        }

        /// <summary>
        /// Performs a file consistency check, where it will make sure each identified ref file matches the current upload policy.
        /// In the future this will also add any missing database entries.
        /// </summary>
        /// <param name="regenBefore">An ISO date string in UTC. Regenerate files if they are before the specified date.</param>
        /// <param name="idRange">Of the form "1-500" inclusive. Will skip any files with an upload ID out of this range if specified. Can be used for bulk task delegation amongst a group of machines. Use blank values for "anything after" ("100-") and "anything before" ("-100").</param>
        [HttpGet("file-consistency")]
        public async ValueTask FileConsistency([FromQuery] string regenBefore = null, [FromQuery] string idRange = null)
        {

            var context = await Request.GetContext();

            if (!context.Role.CanViewAdmin)
            {
                throw PermissionException.Create("file_consistency", context);
            }

            uint minId = 0;
            uint maxId = uint.MaxValue;

            if (!string.IsNullOrWhiteSpace(idRange))
            {
                var parts = idRange.Split('-');

                if (parts.Length == 1)
                {
                    var id = parts[0].Trim();

                    if (!string.IsNullOrEmpty(id))
                    {
                        uint.TryParse(parts[0], out minId);
                        uint.TryParse(parts[0], out maxId);
                    }
                }

                if (parts.Length == 2)
                {
                    var min = parts[0].Trim();
                    var max = parts[1].Trim();
                    if (!string.IsNullOrEmpty(min))
                    {
                        uint.TryParse(parts[0], out minId);
                    }
					if (!string.IsNullOrEmpty(max))
					{
						uint.TryParse(parts[1], out maxId);
					}
				}
			}

            DateTime? regenDate = string.IsNullOrWhiteSpace(regenBefore) ? null : DateTime.Parse(regenBefore);
			await (_service as UploadService).FileConsistency(context, regenDate, minId, maxId);
        }

        /// <summary>
        /// Replace any existing refs with new ones
        /// </summary>
        [HttpGet("replace")]
        public async ValueTask<List<MediaRef>> Replace([FromQuery] string sourceRef, [FromQuery] string targetRef)
        {

            if (string.IsNullOrWhiteSpace(sourceRef))
            {
                throw new PublicException("No source media reference was provided - aborted", "no_sourceRef");
            }

            if (string.IsNullOrWhiteSpace(targetRef))
            {
                throw new PublicException("No target media reference was provided - aborted", "no_targetRef");
            }

            var context = await Request.GetContext();

            if (!context.Role.CanViewAdmin)
            {
                throw PermissionException.Create("ref_replace", context);
            }

            List<MediaRef> mediaRefs = new List<MediaRef>();

            // Get all the current locales:
            var _locales = Services.Get<LocaleService>();
            var locales = await _locales.Where("").ListAll(context);

            foreach (var locale in locales)
            {
                context.LocaleId = locale.Id;

                // loop through all content and attempt to replace ref values
                foreach (var kvp in Services.All)
                {
                    var updated = await kvp.Value.ReplaceRefs(context, sourceRef, targetRef);
                    if (updated != null && updated.Any())
                    {
                        mediaRefs.AddRange(updated);
                    }
                }
            }

            return mediaRefs;
        }

        /// <summary>
        /// Update alt names based on image data
        /// </summary>
        [HttpGet("update-alts")]

        public async void UpdateAlts()
        {
            var context = await Request.GetContext();

            if (!context.Role.CanViewAdmin)
            {
                throw PermissionException.Create("alt_update", context);
            }

            (_service as UploadService).UpdateAltNames(context);
        }

        /// <summary>
        /// Upgrade refs such that any ref fields hold the latest version of a specified ref.
        /// </summary>
        [HttpGet("update-refs")]
        public async ValueTask<List<MediaRef>> UpdateRefs([FromQuery] bool update)
        {
            var context = await Request.GetContext();

            if (!context.Role.CanViewAdmin)
            {
                throw PermissionException.Create("ref_update", context);
            }

            List<MediaRef> mediaRefs = new List<MediaRef>();

            var refMap = new Dictionary<uint, string>();

            // Load every upload with the latest ref
            var allUploads = await _service.Where().ListAll(context);

            foreach (var upload in allUploads)
            {
                refMap[upload.Id] = upload.Ref;
            }

            // Get all the current locales:
            var _locales = Services.Get<LocaleService>();
            var locales = await _locales.Where("").ListAll(context);

            foreach (var locale in locales)
            {
                context.LocaleId = locale.Id;

                // loop through all content and attempt to update refs:
                foreach (var kvp in Services.All)
                {
                    var updated = await kvp.Value.UpdateRefs(context, update, refMap);
                    if (updated != null && updated.Any())
                    {
                        mediaRefs.AddRange(updated);
                    }
                }
            }

            return mediaRefs.OrderBy(s => s.Url).ToList();
        }

        /// <summary>
        /// Preview any media refs changes 
        /// </summary>
        [HttpGet("replace/preview")]
        public async ValueTask<List<MediaRef>> Preview([FromQuery] string uploadRef)
        {
            if (string.IsNullOrWhiteSpace(uploadRef))
            {
                throw new PublicException("No media reference was provided - aborted", "no_ref");
            }

            var context = await Request.GetContext();

            if (!context.Role.CanViewAdmin)
            {
                throw PermissionException.Create("ref_replace", context);
            }

            List<MediaRef> mediaRefs = new List<MediaRef>();

            // Get all the current locales:
            var _locales = Services.Get<LocaleService>();
            var locales = await _locales.Where("").ListAll(context);

            foreach (var locale in locales)
            {
                context.LocaleId = locale.Id;

                // loop through all content and preview the proposed replacement of ref values
                foreach (var kvp in Services.All)
                {
                    var updated = await kvp.Value.ReplaceRefs(context, uploadRef, string.Empty);
                    if (updated != null && updated.Any())
                    {
                        mediaRefs.AddRange(updated);
                    }
                }
            }

            return mediaRefs;
        }
    }
}
