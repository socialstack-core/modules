using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Pages;
using Api.Permissions;
using Api.Revisions;
using Api.Startup;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.AuditEvents;

/// <summary>
/// Handles SS core audit events. This is for tracking general use events which are admin searchable 
/// such as logging in, password changes, impersonation and so on.
/// It is combined with revisions to create a broad picture of activity.
/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
/// </summary>
public partial class AuditEventService : AutoService<AuditEvent>
    {
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public AuditEventService() : base(Events.AuditEvent)
        {
		// Example admin page install:
		InstallAdminPages("Audit Log", "fas:fa-passport", new string[] { "id", "name" });

		Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
		{
			if (builder.ContentType == typeof(AuditEvent) && builder.PageType == CommonPageType.AdminList)
			{
				builder.GetContentRoot()
					.Empty()
					.AppendChild(new CanvasNode("Admin/Audits/List"));
			}

			return new ValueTask<PageBuilder>(builder);
		}, 30); // After all other autoform tabs etc have been added

		// On any service start clear the cache:
		Events.Service.AfterCreate.AddEventListener((Context ctx, AutoService svc) => {
			_cachedTypes = null;
			return new ValueTask<AutoService>(svc);
		});

	}

	private List<AuditEventType> _cachedTypes;

	/// <summary>
	/// Gets a cached set of all possible types to include in the set of audit log.
	/// This set is then filtered by both code and local config.
	/// </summary>
	/// <returns></returns>
	public async ValueTask<List<AuditEventType>> GetTypesToInclude(Context context)
	{
		var cache = _cachedTypes;

		if (cache != null)
		{
			return cache;
		}

		var types = new List<AuditEventType>();

		foreach (var kvp in Services.AutoServices)
		{
			var revService = kvp.Value.GetRevisions();

			if (revService == null || (kvp.Value as RevisionService) != null)
			{
				continue;
			}

			// It's a service with revisions, so it can register as a candidate type.
			var newType = new AuditEventType() {
				Service = kvp.Value,
				TypeName = kvp.Value.EntityName,
				RevisionTypeName = revService.GetEntityName()
			};

			newType = await Events.AuditEvent.OnRegisterType.Dispatch(context, newType);

			if (newType == null)
			{
				// It was rejected entirely.
				continue;
			}

			types.Add(newType);
		}

		_cachedTypes = types;
		return types;
	}

	/// <summary>
	/// Performs a detailed search, combining audit log events with revisions.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="request"></param>
	/// <returns></returns>
	public async ValueTask<AuditLogSearch> DetailedSearch(Context context, AuditLogSearchRequest request)
	{
		var search = new AuditLogSearch() {
			Config = request
		};

		// Cap the range to a max of 1d:
		if (!search.Config.Range.HasValue || search.Config.Range.Value > 1440)
		{
			search.Config.Range = 1440;
		}

		// Set StartUtc if necessary:
		if (!search.Config.StartUtc.HasValue)
		{
			// Now-Range:
			search.Config.StartUtc = DateTime.UtcNow.AddMinutes(-search.Config.Range.Value);
		}

		search = await Events.AuditEvent.Search.Dispatch(context, search);
		return search;
	}
}

/// <summary>
/// A type which can be included in the global audit event log.
/// </summary>
public partial class AuditEventType
{
	/// <summary>
	/// The service for the type.
	/// </summary>
	[JsonIgnore]
	public AutoService Service;

	/// <summary>
	/// E.g. "User"
	/// </summary>
	public string TypeName;

	/// <summary>
	/// E.g. "User_Revisions"
	/// </summary>
	[JsonIgnore]
	public string RevisionTypeName;
}