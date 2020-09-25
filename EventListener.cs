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
							if (int.TryParse(timestamp, out int timestampVal))
							{
								// Validate the signature itself:
								var signedValue = context.Context.Request.Path + "?t=" + timestamp;

								if (_signatureService.ValidateSignature(signedValue, signature))
								{
									return;
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
				var pathToUIDir = AppSettings.Configuration["UI"];
				if (string.IsNullOrEmpty(pathToUIDir))
				{
					pathToUIDir = "UI/public";
				}

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
				var pathToAdminDir = AppSettings.Configuration["Admin"];
				if (string.IsNullOrEmpty(pathToAdminDir))
				{
					pathToAdminDir = "Admin/public/en-admin";
				}

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
			var settingName = priv ? "ContentPrivate" : "Content";
			var contentPath = AppSettings.Configuration[settingName];

			if (string.IsNullOrEmpty(contentPath))
			{
				throw new Exception(
					"You're missing the '" + settingName + 
					"' configuration setting in your appsettings.json. " +
					"This is the path to your content directory which will hold uploads. " +
					"It's usually (public) 'Content/content' or (private) 'Content/content-private' by default. "
				);
			}

			contentPath = Path.GetFullPath(contentPath);

			if (!Directory.Exists(contentPath))
			{
				Directory.CreateDirectory(contentPath);
			}

			return contentPath;
		}

	}
}
