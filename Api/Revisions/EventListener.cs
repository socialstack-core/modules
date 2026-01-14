using Api.AutoForms;
using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Pages;
using Api.Startup;
using Api.Startup.Routing;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;


namespace Api.Revisions
{

	/// <summary>
	/// Listens out for the DatabaseDiff run to add additional revision tables, as well as BeforeUpdate events to then automatically create the revision rows.
	/// </summary>
	[EventListener]
	public class EventListener
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Hook up all before update events for anything which is a RevisionRow type.
			// Essentially before the content actually goes into the database, we copy the database row into the _revisions table (with 1 database-side query), 
			// and bump the Revision number of the about to be updated row.
			
			var methodInfo = GetType().GetMethod(nameof(SetupForRevisions));

			Events.Service.AfterCreate.AddEventListener(async (Context ctx, AutoService svc) => {
				if (svc == null || svc.ServicedType == null)
				{
					return svc;
				}

				// Do nothing if this is a revision service itself.
				var svcType = svc.GetType();

				if (svcType.IsGenericType && svcType.GetGenericTypeDefinition() == typeof(RevisionService<,>))
				{
					return svc;
				}

				var eventGroup = svc.GetEventGroup();

				if (eventGroup != null && ContentTypes.IsAssignableToGenericType(svc.ServicedType, typeof(VersionedContent<>)))
				{
					// Invoke setup for type:
					var idType = svc.IdType;

					var setupType = methodInfo.MakeGenericMethod(new Type[] {
						svc.ServicedType,
						idType
					});

					var valTask = (ValueTask)setupType.Invoke(this, new object[] {
						ctx,
						svc
					});

					await valTask;
				}

				return svc;
			});

			Events.Permalink.BeforeAddTerminal.AddEventListener((Context context, PageTerminalBehaviour pageTerminal) => {

				if (pageTerminal == null || pageTerminal.Page == null)
				{
					return new ValueTask<PageTerminalBehaviour>(pageTerminal);
				}

				if (pageTerminal.Page.PrimaryContentIsRevisions)
				{
					// This page wants its primary content from a revision ID rather than an actual piece of content.
					// To do that we'll make the page terminal instance with a node which loads a revision first.
					pageTerminal.GenericPageTerminalType = typeof(RouterRevisionPageTerminal<,>);
				}

				return new ValueTask<PageTerminalBehaviour>(pageTerminal);
			});

			PageService pageService = null;

			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder == null)
				{
					return new ValueTask<PageBuilder>(builder);
				}

				if (
					builder.IsAdmin && 
					builder.PageType == CommonPageType.AdminEdit && 
					builder.ContentType != null && 
					ContentTypes.IsAssignableToGenericType(builder.ContentType, typeof(VersionedContent<>)))
				{
					// It's an edit page for something which supports revisions. 
					// We need to also request another edit page for displaying the revisions themselves.
					pageService ??= Services.Get<PageService>();
					var typeName = builder.ContentType.Name;
					var typeNameLowercase = typeName.ToLower();

					// "BlogPost" -> "Blog Post".
					var tidySingularName = Api.Startup.Pluralise.NiceName(typeName);
					var tidyPluralName = Api.Startup.Pluralise.Apply(tidySingularName);
					var options = builder.AdminPageOptions;
					
					var singlePage = new PageBuilder
					{
						ContentType = builder.ContentType,
						PageType = CommonPageType.AdminRevisionEdit,
						PrimaryContentType = typeName,
						PrimaryContentIncludes = builder.PrimaryContentIncludes,
						Url = "/en-admin/" + typeNameLowercase + "/revision/${" + typeNameLowercase + ".id}",
						Key = "admin_" + typeNameLowercase + "_revision_edit",
						Title = "Editing revision",
						BuildBody = (PageBuilder builder) =>
						{
							var singlePageCanvas = new CanvasNode("Admin/AutoForm")
								.With("contentType", typeName)
								.With("singular", tidySingularName)
								.With("isRevision", true)
								.With("plural", tidyPluralName);

							if (options.Tabs != null && options.Tabs.Count > 0)
							{
								singlePageCanvas.With("tabs", new List<AdminTab>(options.Tabs));
							}

							singlePageCanvas.WithPrimaryLink("content");

							return builder.AddTemplate(
								singlePageCanvas
							);
						},
						PrimaryContentIsRevisions = true
					};

					pageService.Install(singlePage);

					// Also need to add the recentDraft include to the source edit page itself:
					if (string.IsNullOrEmpty(builder.PrimaryContentIncludes))
					{
						builder.PrimaryContentIncludes = "recentDraft";
					}
					else
					{
						builder.PrimaryContentIncludes += ",recentDraft";
					}
				}

				return new ValueTask<PageBuilder>(builder);
			});

			Events.AutoForm.BuildMeta.AddEventListener((Context context, AutoFormInfo autoForm, AutoService service) => {

				autoForm.SupportsRevisions = service.GetRevisions() != null;

				return new ValueTask<AutoFormInfo>(autoForm);
			});
		}

		/// <summary>
		/// Sets a particular type with revision handlers. Used via reflection.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="context"></param>
		/// <param name="autoService"></param>
		public async ValueTask SetupForRevisions<T, ID>(Context context, AutoService<T, ID> autoService)
			where T : VersionedContent<ID>, new()
			where ID: struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// Spawn the revisions service:
			autoService.Revisions = new RevisionService<T, ID>(autoService);

			// Tell the system that this service has started. The main side effect we're after here
			// is for whichever data service is in use to mount it and create whatever data storage mechanism it needs.
			// Note that this is itself called from AfterCreate, so our handler explicitly looks out for & then no-ops 
			// when it spots the revisions on revisions situation.
			await Services.StateChange(true, autoService.Revisions);

			var contentType = autoService.InstanceType;
			var evtGroup = autoService.EventGroup;

			// Invoked by reflection

			evtGroup.BeforeUpdate.AddEventListener((Context context, T content, T original) =>
			{
				if (content == null)
				{
					return new ValueTask<T>(content);
				}

				// Bump its revision number.
				content.Revision++;

				return new ValueTask<T>(content);
			}, 11);

			evtGroup.AfterCreate.AddEventListener(async (Context context, T content) =>
			{
				if (content == null)
				{
					return content;
				}

				var now = DateTime.UtcNow;

				var contentJson = await autoService.ToStoredJson(content);

				var rev = new Revision<T, ID>()
				{
					UserId = content.UserId,
					CreatedUtc = now,
					EditedUtc = now,
					ContentId = content.Id,
					ContentJson = contentJson,
					ImpersonatorUserId = context.RealUserId,
					ActionType = 1
				};

				await autoService.Revisions.Create(context, rev, DataOptions.IgnorePermissions);

				return content;
			}, 11);

			evtGroup.AfterUpdate.AddEventListener(async (Context context, T content) => 
			{
				if (content == null)
				{
					return content;
				}

				var now = DateTime.UtcNow;

				var contentJson = await autoService.ToStoredJson(content);

				var rev = new Revision<T, ID>() {
					UserId = content.UserId,
					CreatedUtc = now,
					EditedUtc = now,
					ContentId = content.Id,
					ImpersonatorUserId = context.RealUserId,
					ContentJson = contentJson,
					ActionType = 2
				};

				await autoService.Revisions.Create(context, rev, DataOptions.IgnorePermissions);

				return content;
			});

			evtGroup.AfterDelete.AddEventListener(async (Context context, T content) =>
			{
				if (content == null)
				{
					return content;
				}

				var now = DateTime.UtcNow;

				var contentJson = await autoService.ToStoredJson(content);

				var rev = new Revision<T, ID>()
				{
					UserId = content.UserId,
					ImpersonatorUserId = context.RealUserId,
					CreatedUtc = now,
					EditedUtc = now,
					ContentId = content.Id,
					ContentJson = contentJson,
					ActionType = 3
				};

				await autoService.Revisions.Create(context, rev, DataOptions.IgnorePermissions);

				return content;
			}, 11);
			
		}

	}
}
