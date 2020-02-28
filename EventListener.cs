using System;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.DatabaseDiff;
using Api.Eventing;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


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
			var revisionIdField = typeof(RevisionRow).GetField("_RevisionId", BindingFlags.Instance | BindingFlags.NonPublic);
			var isDraftField = typeof(RevisionRow).GetField("_IsDraft", BindingFlags.Instance | BindingFlags.NonPublic);
			
			// Hook up the database diff event, which will be used to generate tables for us:
			Events.DatabaseDiffBeforeAdd.AddEventListener((Context ctx, FieldMap fieldMap, Type typeInfo, Schema newSchema) => {

				// If this type *is* RevisionRow, return null.
				// That blocks it from generating any table.
				if (typeInfo == typeof(RevisionRow))
				{
					return Task.FromResult((FieldMap)null);
				}
				
				// Firstly, is this type a RevisionRow?
				// If so, we'll need to add another table to the schema with the same set of fields only it's called _revisions.

				if (!typeof(RevisionRow).IsAssignableFrom(typeInfo))
				{
					return Task.FromResult(fieldMap);
				}

				// We've got a revisionable content type. Add a revisions table to the schema:

				var targetTableName = typeInfo.TableName() + "_revisions";
				Field idField = null;

				// All of the fields in the type can be revisioned, so we add them all.
				// Note, however, that the Id column changes meaning. It becomes the Id of the *revision*.
				foreach (var field in fieldMap.Fields)
				{
					DatabaseColumnDefinition columnDefinition = null;
					
					// If we have the Id field..
					if (field.Name == "Id")
					{
						// Special case for the Id field, as it'll be the revision ID.
						// That means when we load it, it must go into the RevisionId field instead.
						// We can do that by cloning the field object and then simply changing the FieldInfo for the field it sets.
						idField = field;
						var specialIdField = field.Clone();

						// The field name stays the same, so it's still "Id" in the table, but the value goes to and from the RevisionId field.
						specialIdField.TargetField = revisionIdField;

						// Create a column definition:
						columnDefinition = new DatabaseColumnDefinition(specialIdField, targetTableName);
						columnDefinition.IsAutoIncrement = true;
					}
					else
					{
						// Create a column definition:
						columnDefinition = new DatabaseColumnDefinition(field, targetTableName);
					}

					// Add to target schema:
					newSchema.Add(columnDefinition);
				}

				if (idField != null)
				{
					// Next, as the Id column means something else, we'll need to add a special column for the content's ID.
					// Similarly to above, we can do that by cloning the Id field but this time we give it a new name.
					// That'll mean its value goes to and from the Id field on objects, but it is retained in a column called RevisionOriginalContentId.
					var contentIdField = idField.Clone();
					contentIdField.Name = "RevisionOriginalContentId";
					contentIdField.SetFullName();

					var contentIdColumn = new DatabaseColumnDefinition(
						contentIdField,
						targetTableName
					);

					// It may have seen that the Id column is autoinc, depending on field attributes, so clear that:
					contentIdColumn.IsAutoIncrement = false;

					newSchema.Add(
						contentIdColumn
					);
				}

				// Add IsDraft column:
				var isDraft = new Field()
				{
					OwningType = typeInfo,
					Type = isDraftField.FieldType,
					TargetField = isDraftField,
					Name = "RevisionIsDraft"
				};

				isDraft.SetFullName();

				newSchema.Add(new DatabaseColumnDefinition(isDraft, targetTableName));

				return Task.FromResult(fieldMap);
			});

			// Next hook up all before update events for anything which is a RevisionRow type.
			// Essentially before the content actually goes into the database, we copy the database row into the _revisions table (with 1 database-side query), 
			// and bump the Revision number of the about to be updated row.
			var allBeforeUpdateEvents = Events.FindByPlacementAndVerb(EventPlacement.Before, "Update");

			// The database service:
			IDatabaseService database = null;

			foreach (var beforeUpdateEvent in allBeforeUpdateEvents)
			{
				// Is this update for a RevisionRow type?
				var contentType = beforeUpdateEvent.PrimaryType;

				if (!typeof(RevisionRow).IsAssignableFrom(contentType))
				{
					// nope
					continue;
				}

				// We've got a BeforeUpdate event for revisionable content.

				// Create the original field map:
				var fieldMap = new FieldMap(contentType);
				
				// First, generate a 'copy' query. It'll transfer values from table A to table B.
				var transferMap = new FieldTransferMap();
				transferMap.TargetTypeNameExtension = "_revisions";

				// For each field in the map, create a transfer:
				foreach (var field in fieldMap.Fields)
				{
					// Special case for the Id field.
					if (field.Name == "Id")
					{
						// Id transfers to the RevisionContentId field.
						transferMap.Add(contentType, field.Name, contentType, "RevisionOriginalContentId");
					}
					else
					{
						// The only difference is the target type name extension above.
						transferMap.Add(contentType, field.Name, contentType, field.Name);
					}
				}
				
				transferMap.AddConstant(contentType, "RevisionIsDraft", false);
				
				// The query itself:
				var copyQuery = Query.Copy(transferMap);
				copyQuery.Where().EqualsArg(contentType, "Id", 0);

				var str = copyQuery.GetQuery();

				// And add an event handler now:
				beforeUpdateEvent.AddEventListener(async (Context context, object[] args) =>
				{
					if (args == null || args.Length == 0)
					{
						return null;
					}

					var revisionableContent = args[0] as RevisionRow;

					if (revisionableContent == null)
					{
						// This event is not for us
						return args[0];
					}

					if (database == null)
					{
						database = Services.Get<IDatabaseService>();
					}
					
					/*
					// trigger the before create revision events:
					if(beforeUpdateEvent.EventGroup != null){
						beforeUpdateEvent.EventGroup.RevisionBeforeCreate(context, revisionableContent);
					}
					*/

					// Run the copy query now:
					await database.Run(copyQuery, revisionableContent.Id);

					#warning TODO - trigger the before and after events
					// - Requires collecting the ID from the above copy call
					// - Also requires collecting the EventGroup that the update event came from in order to call the events.

					/*
					// trigger the after create revision events:
					if(beforeUpdateEvent.EventGroup != null){
						// Note: This will not know what the revisions ID is.
						beforeUpdateEvent.EventGroup.RevisionAfterCreate(context, revisionableContent);
					}
					*/

					// Bump its revision number.
					revisionableContent.Revision++;

					return args[0];
				}, 11);

			}
		}
		
	}
}
