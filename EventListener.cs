using Api.Contexts;
using Api.Database;
using Api.DatabaseDiff;
using Api.Eventing;
using Api.Startup;
using System;
using System.Threading.Tasks;

namespace Api.Users
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
			Events.DatabaseDiffBeforeAdd.AddEventListener((Context ctx, FieldMap fieldMap, Type typeInfo, Schema newSchema) => {
				// If this type *is* RevisionRow or UserCreatedRow, return null.
				// That blocks it from generating any table.
				if (typeInfo == typeof(RevisionRow) || typeInfo == typeof(UserCreatedRow))
				{
					return Task.FromResult((FieldMap)null);
				}
				
				return Task.FromResult(fieldMap);
			}, 1);
		}
	}
}