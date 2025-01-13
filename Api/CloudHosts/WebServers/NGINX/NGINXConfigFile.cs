using Api.Startup;
using System.Collections.Generic;
using System.IO;

namespace Api.CloudHosts;


/// <summary>
/// A class which represents and can generate NGINX configuration files.
/// An nginx config file is essentially just an NGINX "context" (otherwise known as a block in typical coding languages).
/// </summary>
public partial class NGINXConfigFile : NGINXContext
{
    private NGINXContext _configurableContext;

    /// <summary>
    /// Convenience version of WriteToFile, writing to a standard file based on the determined environment.
    /// </summary>
    public void WriteToFile()
	{
		// Write nginx.conf to the working directory (alongside the socket files on a deployed Linux server).
		File.WriteAllText("./nginx.conf", ToString());

		if (_configurableContext != null)
		{
			File.WriteAllText("./nginx/nginx-url-config.conf", _configurableContext.ToString());
		}
	}

	/// <summary>
	/// Adds a server context as a child of this file.
	/// A server context is e.g. "server { ...}" in the config file.
	/// </summary>
	public NGINXContext AddServerContext()
	{
		return AddContext("server");
	}

	/// <summary>
	/// Gets the context into which you should add your custom config. Only available after setting up defaults.
	/// </summary>
	/// <returns></returns>
	public NGINXContext GetConfigurableContext()
	{
		return _configurableContext;
	}

	/// <summary>
	/// Sets up the default NGINX config for *this* server.
	/// It will generate different things depending on which environment the server is (stage/ prod mainly).
	/// </summary>
	public void SetupDefaults(List<string> hostnames = null)
	{
		hostnames ??= new List<string>() { "_" };

		// TODO: Does not handle www and root domain redirects yet.
		// What that means: Check the server's configured domains. If one of them is a subdomain of another, then we have a root domain redirect requirement.

		// Part 1. Default port 80 (non https) redirect. On sites that do not have www, this is always the same:
		var httpToHttpsMain = AddServerContext();

		// http->https doesn't actually care what the hostname was. There's only 1 of these regardless of the provided hostname set.
		httpToHttpsMain
			.AddDirective("listen", "80 default_server")       // Listen on IPv4 port 80 (http)
			.AddDirective("listen", "[::]:80 default_server") // Listen on IPv6 port 80 (http)
			.AddDirective("charset", "utf-8") // tell nginx we want http to be in utf-8 always
			.AddDirective("index", "index.html index.php") // SS doesn't use this anymore, but is present for old site backwards compatibility
			.AddDirective("error_log", "/dev/null") // Don't log errors in the http->https redirect. 
			.AddDirective("access_log", "/dev/null") // Don't log each request in the http->https redirect.
			.AddDirective("server_name", "_"); // Underscore is the NGINX "default" server name.
											   // It tells nginx that we don't actually care what the domain name is here - redirect any domain to https. Keeps us generic so far.
		
		// On to the location contexts for http->https:
        httpToHttpsMain
            .AddLocationContext("/")
                 .AddDirective("return", "301 https://$http_host$request_uri"); // Permanent redirect to https. We're never going to change from https -> http,

        var fileRootPath = Services.IsStaging() ? "/var/www/stage" : "/var/www/prod";

		// Time for the HTTPS listeners (the actual workhorses) next.
		for (var i=0;i<hostnames.Count;i++)
		{
			var hostname = hostnames[i];

			// Create the https contexts:
			var httpsContext = AddServerContext();

			httpsContext
				.AddDirective("listen", "443 ssl http2" + (i == 0 ? " default_server" : ""))       // Listen on IPv4 port 443 (https) with HTTP/2 support
				.AddDirective("listen", "[::]:443 ssl http2" + (i == 0 ? " default_server" : "")) // Listen on IPv6 port 443 (https) with HTTP/2 support
				.AddDirective("ssl_certificate", "./nginx/" + hostname + "-fullchain.pem") // Certificate to use.
				.AddDirective("ssl_certificate_key", "./nginx/" + hostname + "-privkey.pem") // Its key
				.AddDirective("server_name", hostname)
				.AddDirective("include", "./nginx/nginx-url-config.conf");
		}

		var cfgContext = new NGINXContext();
		_configurableContext = cfgContext;

		cfgContext.AddDirective("server_tokens", "off")
			.AddDirective("charset", "utf-8") // tell nginx we want http to be in utf-8 always
			.AddDirective("index", "index.html index.json index.php") // SS doesn't use this anymore, but is present for old site backwards compatibility
			.AddDirective("root", "\"" + fileRootPath + "/UI/public\"") // Location of the "root". Static file paths are served from this root.
			.AddDirective("client_max_body_size", "5G") // Max upload size of 5GB.
			.AddDirective("error_page", "404 /error.html") // Error page location (not used anymore)
			.AddDirective("error_page", "500 502 503 504 /error.html"); // Error page location (not used anymore either)

		// The admin panel
		cfgContext
			.AddLocationContext("~ ^/en-admin")
				.AddDirective("gzip_static", "on") // If a .gz file exists, use it directly.
				.AddDirective("add_header", "Cache-control no-store") // No caching on the admin panel API pages please!
				.AddDirective("add_header", "Pragma no-cache") // I'm serious, no caching or else!
				.AddDirective("add_header", "X-Content-Type-Options \"nosniff\"") // Don't make content type guesses based on the first few bytes.
				.AddDirective("add_header", "X-Frame-Options sameorigin") // Only "this" site is allowed to embed the admin panel in an iframe.
				.AddDirective("proxy_pass", "http://unix:" + fileRootPath + "/api.sock") // The real meat: Requests for admin panel pages should be passed to the C# API.
				.AddDirective("proxy_http_version", "1.1") // NGINX proxy pass config - use HTTP/1.1
				.AddDirective("proxy_set_header", "Host $host"); // Set the Host response header

		// Content directory - serve public uploads
		cfgContext
			.AddLocationContext("~ ^/content/")
				.AddDirective("gzip_static", "on") // If a .gz file exists, use it directly.
				.AddDirective("expires", "max") // The opposite of what the admin panel does - cache as long as you can!
				.AddDirective("add_header", "X-Frame-Options sameorigin") // Only "this" site is allowed to embed content files in an iframe.
				.AddDirective("add_header", "Referrer-Policy strict-origin-when-cross-origin") // Only "this" site is allowed to embed content files in an iframe.
				.AddDirective("add_header", "Cache-Control public") // Any proxies between us and the end user - the public internet - can cache content files too.
				.AddDirective("try_files", "$uri $uri/ index.html") // Attempt to actually load the file from the content folder
				.AddDirective("root", "\"" + fileRootPath + "/Content\""); // What content folder you say? why, this one of course!

		// Pack directory - serve static UI assets (fonts etc)
		cfgContext
			.AddLocationContext("~ ^/pack/static/")
				.AddDirective("gzip_static", "on") // If a .gz file exists, use it directly.
				.AddDirective("expires", "max") // The opposite of what the admin panel does - cache as long as you can!
				.AddDirective("add_header", "X-Frame-Options sameorigin") // Only "this" site is allowed to embed content files in an iframe.
				.AddDirective("add_header", "Referrer-Policy strict-origin-when-cross-origin") // Only "this" site is allowed to embed content files in an iframe.
				.AddDirective("add_header", "Cache-Control public");

		// Content directory - serve *private* uploads
		cfgContext
			.AddLocationContext("~ ^/content-private/")
				.AddDirective("proxy_pass", "http://unix:" + fileRootPath + "/api.sock") // Private uploads are served via the C# API such that it can actually enforce access rules.
				.AddDirective("proxy_http_version", "1.1") // NGINX proxy pass config - use HTTP/1.1
				.AddDirective("proxy_set_header", "Host $host") // Set the Host response header
				.AddDirective("proxy_cache_bypass", "$http_upgrade")
				.AddDirective("proxy_set_header", "Connection keep-alive")
				.AddDirective("add_header", "Cache-control no-store") // Private uploads are deemed sensitive content so no caching please!
				.AddDirective("add_header", "Pragma no-cache"); // No caching, I mean it!

		// /v1/ - actual API endpoints
		cfgContext
			.AddLocationContext("~ ^/v1/")
				.AddDirective("proxy_pass", "http://unix:" + fileRootPath + "/api.sock") // Send straight to the C# API.
				.AddDirective("proxy_http_version", "1.1") // NGINX proxy pass config - use HTTP/1.1
				.AddDirective("proxy_set_header", "Host $host") // Set the Host response header
				.AddDirective("proxy_cache_bypass", "$http_upgrade")
				.AddDirective("proxy_set_header", "Connection keep-alive");

		// /live-websocket/ - the websocket service which has its own UNIX socket to listen to.
		cfgContext
			.AddLocationContext("~ ^/live-websocket/")
				.AddDirective("proxy_pass", "http://unix:" + fileRootPath + "/ws.sock") // Send to the C# API but specifically the websocket server socket.
				.AddDirective("proxy_http_version", "1.1") // NGINX proxy pass config - use HTTP/1.1
				.AddDirective("proxy_set_header", "Host $host") // Set the Host response header
				.AddDirective("proxy_send_timeout", "7d") // Prevent NGINX gateway timeout if no bytes are sent for a short period of time. In practise this never happens anymore as the server sends a heartbeat.
				.AddDirective("proxy_read_timeout", "7d") // Prevent NGINX gateway timeout if no bytes are rcvd for a short period of time. In practise this never happens anymore as the client sends a heartbeat.
				.AddDirective("proxy_set_header", "Upgrade $http_upgrade")
				.AddDirective("proxy_set_header", "Connection $connection_upgrade");

		// in memory .js and .css assets
		cfgContext
			.AddLocationContext("~ ^/pack/")
				.AddDirective("gzip_static", "on") // If a .gz file exists, use it directly.
				.AddDirective("expires", "max")
				.AddDirective("add_header", "X-Frame-Options sameorigin") // Only "this" site is allowed to embed the UI in an iframe.
				.AddDirective("add_header", "Referrer-Policy strict-origin-when-cross-origin") // Only "this" site is allowed to embed the UI in an iframe.
				.AddDirective("add_header", "Cache-control public")
				.AddDirective("proxy_pass", "http://unix:" + fileRootPath + "/api.sock") // The real meat: Requests for admin panel pages should be passed to the C# API.
				.AddDirective("proxy_http_version", "1.1") // NGINX proxy pass config - use HTTP/1.1
				.AddDirective("proxy_set_header", "Host $host"); // Set the Host response header

		// favicons
		cfgContext
			.AddLocationContext("~ ^/favicon")
				.AddDirective("gzip_static", "on") // If a .gz file exists, use it directly.
				.AddDirective("expires", "max")
				.AddDirective("add_header", "X-Frame-Options sameorigin") // Only "this" site is allowed to embed the UI in an iframe.
				.AddDirective("add_header", "Referrer-Policy strict-origin-when-cross-origin") // Only "this" site is allowed to embed the UI in an iframe.
				.AddDirective("add_header", "Cache-control public")
				.AddDirective("try_files", "$uri $uri/ index.html"); // Load from filesys

		// Everything else - the frontend pages.
		cfgContext
			.AddLocationContext("/")
				.AddDirective("gzip_static", "on") // If a .gz file exists, use it directly.
				.AddDirective("add_header", "X-Frame-Options sameorigin") // Only "this" site is allowed to embed the UI in an iframe.
				.AddDirective("add_header", "X-XSS-Protection \"1; mode=block\"") // Only "this" site is allowed to embed the UI in an iframe.
				.AddDirective("add_header", "Referrer-Policy strict-origin-when-cross-origin") // Only "this" site is allowed to embed the UI in an iframe.
				.AddDirective("add_header", "X-Content-Type-Options \"nosniff\"") // Don't make content type guesses based on the first few bytes.
				.AddDirective("add_header", "Strict-Transport-Security \"max-age=31536000; includeSubDomains\"") // STS: "only ever use HTTPS to talk to this site and my subdomains".
				.AddDirective("proxy_pass", "http://unix:" + fileRootPath + "/api.sock") // The real meat: Requests for admin panel pages should be passed to the C# API.
				.AddDirective("proxy_http_version", "1.1") // NGINX proxy pass config - use HTTP/1.1
				.AddDirective("proxy_set_header", "Host $host"); // Set the Host response header

		// An NGINX mapping which defines the websocket upgrade behaviour.
		// There's only one of these per entire NGINX config file.
		AddContext("map", "$http_upgrade $connection_upgrade")
			.AddDirective("default", "upgrade")
			.AddDirective("''", "close");

	}


}