using System;
using System.Collections.Generic;
using System.Linq;
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


namespace Api.Translate
{

	/// <summary>
	/// Listens out for the DatabaseDiff run to add additional columns.
	/// It finds any [Localized] fields and adds copies of the field for each locale, except they're always nullable.
	/// </summary>
	[EventListener]
	public class EventListener
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Hook up the database diff event, which will be used to generate tables for us:
			Events.DatabaseDiffBeforeAdd.AddEventListener(async (Context ctx, FieldMap fieldMap, Type typeInfo, Schema newSchema) => {

				if (fieldMap == null)
				{
					return fieldMap;
				}
				
				// Does this field map have any [Localized] fields?
				List<Locale> locales = null;

				var _database = Services.Get<DatabaseService>();

				try
				{
					locales = await _database.List(new Context(), Query.List<Locale>(), null);
				}
				catch
				{
					// The table doesn't exist. Locale set is just the default one:
					locales = new List<Locale>();
				}

				if (locales.Count == 0)
				{
					locales.Add(new Locale()
					{
						Code = "en",
						Name = "English",
						Id = 1
					});
				}

				// Find the max ID:
				var maxId = locales.Max(locale => locale.Id);

				var localeLookup = new Locale[maxId];

				foreach (var locale in locales)
				{
					localeLookup[locale.Id - 1] = locale;
				}

				// Set the available locales:
				_database.Locales = localeLookup;

				// Iterate backwards for simplicity because we add to fields and order doesn't matter:
				for (var fm=fieldMap.Fields.Count-1; fm >= 0; fm--)
				{
					var field = fieldMap.Fields[fm];

					if (field == null || field.TargetField == null)
					{
						continue;
					}

					var fieldIsLocalised = field.TargetField.GetCustomAttribute<LocalizedAttribute>();

					if (fieldIsLocalised == null)
					{
						continue;
					}

					// Got localised fields. Add dupes for each locale now.
					for (var i = 0; i < locales.Count; i++)
					{
						if (locales[i] == null || i == 0)
						{
							continue;
						}

						// Clone the field:
						var clonedField = field.Clone();
						clonedField.Name += "_" + locales[i].Code;
						clonedField.SetFullName();

						// This field must be nullable; translations aren't required:
						if (clonedField.Type.IsValueType && Nullable.GetUnderlyingType(clonedField.Type) == null)
						{
							clonedField.Type = typeof(Nullable<>).MakeGenericType(clonedField.Type);
						}

						fieldMap.Add(clonedField);
						newSchema.Add(new DatabaseColumnDefinition(clonedField, field.OwningType.TableName()));
					}
					
				}
				
				return fieldMap;
			}, 5); // Before revisions, because we want the fields to be revision capable.
			
		}
		
	}
}
