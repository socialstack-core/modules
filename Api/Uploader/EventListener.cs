using System;
using System.IO;
using Api.Startup;
using Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using Api.Signatures;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Api.Uploader
{

	/// <summary>
	/// Listens for various events to setup the auth system.
	/// </summary>
	[EventListener]
	public class EventListener
	{

		private SignatureService _signatureService;

		/// <summary>
		/// Maximum amount of ticks that can occur before a private file timestamp expires. Default is 1 hour worth.
		/// </summary>
		public const long MaxTimestampTicks = 60 * 60 * (long)10000000; // There are 10m ticks in one second.

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Also hook up the configure app method:
			Api.Startup.WebServerStartupInfo.OnConfigureApplication += (IApplicationBuilder app) => {

				/*
				 Note! These are here for convenience and dependency reduction.
				 You should definitely front Kestrel with a full feature web server like NGINX
				 and make it handle the content and UI paths instead.
				 Private files should use NGINX subrequest authentication via the /v1/upload/authenticate endpoint.
				 */

				// Setup the public unauthed static content route:
				var pubPath = SetupDirectory(false);

				var extensions = new FileExtensionContentTypeProvider();

				// DASH mime types:
				extensions.Mappings[".mpd"] = "application/dash+xml";
				extensions.Mappings[".m4s"] = "video/iso.segment";
				extensions.Mappings[".m3u8"] = "application/x-mpegURL";
				
				// Image formats:
				extensions.Mappings[".apng"] = "image/png";
				extensions.Mappings[".webp"] = "image/webp";
				extensions.Mappings[".avif"] = "image/avif";
				extensions.Mappings[".heic"] = "image/heic";
				extensions.Mappings[".heif"] = "image/heif";
				
				// GLTF:
				extensions.Mappings[".gltf"] = "model/gltf+json";
				extensions.Mappings[".glb"] = "model/gltf-binary";
				extensions.Mappings[".hdr"] = "application/octet-stream";
				
				
				// WebVTT:
				extensions.Mappings[".vtt"] = "text/vtt";

				app.UseStaticFiles(new StaticFileOptions()
				{
					FileProvider = new PhysicalFileProvider(pubPath),
					RequestPath = new PathString("/content"),
					ContentTypeProvider = extensions
				});

				// Setup the private authed path:
				var privPath = SetupDirectory(true);

				app.UseStaticFiles(new StaticFileOptions()
				{
					FileProvider = new PhysicalFileProvider(privPath),
					RequestPath = new PathString("/content-private"),
					ContentTypeProvider = extensions,
					OnPrepareResponse = (StaticFileResponseContext context) => {

						if (_signatureService == null)
						{
							_signatureService = Services.Get<SignatureService>();
						}

						// A signature is required. Validate it here.
						// The url is of the form /content-private/x/original.pdf?t=123123&s=ExF221..
						// where 't' is the timestamp the signature was generated and 's' is the signature for the URL including the timestamp.
						var queryFields = context.Context.Request.Query;
						var signature = queryFields["s"];
						var timestamp = queryFields["t"];
						
						if (!string.IsNullOrWhiteSpace(signature) && !string.IsNullOrWhiteSpace(timestamp))
						{
							if (long.TryParse(timestamp, out long timestampVal))
							{
								// Timestamp delta:
								var deltaTime = DateTime.UtcNow.Ticks - timestampVal;

								if (deltaTime <= MaxTimestampTicks)
								{
									var path = context.Context.Request.Path.Value + context.Context.Request.QueryString.Value;
									var endingStart = path.IndexOf('.');
									var idEnd = path.LastIndexOf('-');
									var idStart = 16; // This is the index of the / just after content-private

									var signedRef = "private:" + path.Substring(idStart + 1, idEnd - idStart - 1) + path.Substring(endingStart);

									// Validate the signature itself:
									if (_signatureService.ValidateHmac256AlphaChar(signedRef))
									{
										return;
									}
								}
							}
						}

						/*
						 * Unclear if it's possible to send just these headers, 
						 * but aborting it does at least prevent the file (or any meta about it) from leaking.
						 * 
						context.Context.Response.Headers.ContentLength = 0;
						context.Context.Response.Headers.Remove("ETag");
						context.Context.Response.Headers.Remove("Last-Modified");
						context.Context.Response.Headers.Remove("Content-Type");
						context.Context.Response.StatusCode = 403;
						*/
						context.Context.Abort();
					}
				});

				var autoCreateGzips = false;

				// UI path next:
				var pathToUIDir = AppSettings.GetString("UI", "UI/public");

				pathToUIDir = Path.GetFullPath(pathToUIDir);

				if (!Directory.Exists(pathToUIDir))
				{
					Directory.CreateDirectory(pathToUIDir);
				}
				
				app.UseStaticFiles(new StaticFileOptions()
				{
					FileProvider = new GzipMappingFileProvider(autoCreateGzips, new PhysicalFileProvider(pathToUIDir)),
					RequestPath = new PathString(""),
					OnPrepareResponse = GzipMappingFileProvider.OnPrepareResponse
				});

				// And the admin panel:
				var pathToAdminDir = AppSettings.GetString("Admin", "Admin/public/en-admin");

				pathToAdminDir = Path.GetFullPath(pathToAdminDir);

				if (!Directory.Exists(pathToAdminDir))
				{
					Directory.CreateDirectory(pathToAdminDir);
				}

				app.UseStaticFiles(new StaticFileOptions()
				{
					FileProvider = new GzipMappingFileProvider(autoCreateGzips, new PhysicalFileProvider(pathToAdminDir)),
					RequestPath = new PathString("/en-admin"),
					OnPrepareResponse = GzipMappingFileProvider.OnPrepareResponse
				});

			};

		}
		
		private string SetupDirectory(bool priv)
		{
			var contentPath = priv ? "Content/content-private/" : "Content/content/";
			
			contentPath = Path.GetFullPath(contentPath);

			if (!Directory.Exists(contentPath))
			{
				Directory.CreateDirectory(contentPath);
			}

			return contentPath;
		}

	}
}
