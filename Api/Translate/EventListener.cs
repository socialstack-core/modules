using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Startup;


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
				locales = await Events.Locale.InitialList.Dispatch(ctx, locales);

				// Find the max ID:
				var maxId = locales.Max(locale => locale.Id);

				var localeLookup = new Locale[maxId];

				foreach (var locale in locales)
				{
					localeLookup[locale.Id - 1] = locale;
				}

				// Set the available locales:
				ContentTypes.Locales = localeLookup;

				// Iterate backwards for simplicity because we add to fields and order doesn't matter:
				for (var fm=fieldMap.Fields.Count-1; fm >= 0; fm--)
				{
					var field = fieldMap.Fields[fm];

					if (field == null || field.TargetField == null)
					{
						continue;
					}

					var customFieldAttributes = ContentField.BuildAttributes(field.TargetField.CustomAttributes);

					var fieldIsLocalised = field.TargetField.GetCustomAttribute<LocalizedAttribute>() != null
						|| (customFieldAttributes != null && customFieldAttributes.FirstOrDefault(attr => attr is LocalizedAttribute) != null);

					if (!fieldIsLocalised)
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

						newSchema.AddColumn(clonedField);
					}
					
				}
				
				return fieldMap;
			}, 5); // Before revisions, because we want the fields to be revision capable.
			
		}
		
	}
}
