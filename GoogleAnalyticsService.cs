using Api.Startup;
using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using System.Threading.Tasks;

namespace Api.GoogleAnalytics
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class GoogleAnalyticsService : AutoService
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public GoogleAnalyticsService()
        {
			var config = GetConfig<GoogleAnalyticsServiceConfig>();
			
			Events.Page.Generated.AddEventListener((Context ctx, Document pageDocument) =>
			{
				var gaId = config.Id;
				
				var script2 = new DocumentNode("script")
				.AppendChild(new TextNode(@"window.dataLayer = window.dataLayer || [];
					function gtag() { dataLayer.push(arguments); }
					window.cookieLayer = window.cookieLayer || [];
					function cookieState(m) {
						cookieLayer.push(m);
					}
					cookieState(function (s) {
						if (!s.all && !s.stats) { return; }
						gtag('js', new Date());
						gtag('config', '"+ gaId + @"');
					});"
				));
				pageDocument.MainJs.Parent.InsertBefore(script2, pageDocument.MainJs);

				// This can just go at the end:
				var remoteScript = new DocumentNode("script").With("defer").With("async").With("src", "https://www.googletagmanager.com/gtag/js?id=" + gaId);
				pageDocument.Body.AppendChild(remoteScript);

				return new ValueTask<Document>(pageDocument);
			});
		}
	}
    
}