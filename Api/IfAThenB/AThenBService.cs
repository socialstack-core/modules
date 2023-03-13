using Api.CanvasRenderer;
using Api.Contexts;
using Api.Eventing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;


namespace Api.IfAThenB
{
	/// <summary>
	/// Handles a then b rules.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class AThenBService : AutoService<AThenB>
	{
		private EventList _eventListCache;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AThenBService() : base(Events.AThenB)
		{

			// Create admin pages if they don't already exist:
			InstallAdminPages("If A Then B", "fa:fa-long-arrow-alt-right", new string[] { "id", "name", "description" });

			Events.Service.AfterStart.AddEventListener((Context context, object src) =>
			{

				// A service has started up - clear the event list cache:
				_eventListCache = null;

				return new ValueTask<object>(src);
			});

			Events.Service.BeforeDelete.AddEventListener((Context context, AutoService svc) =>
			{

				// A service has halted - clear the event list cache:
				_eventListCache = null;

				return new ValueTask<AutoService>(svc);
			});

		}

		/// <summary>
		/// The current list of available events.
		/// Updates if a new type is installed.
		/// </summary>
		/// <returns></returns>
		public EventList GetEventList()
		{
			if (_eventListCache != null)
			{
				return _eventListCache;
			}

			var elc = new EventList();

			// Map of system type -> index in the event list array.
			var uniqueTypes = new Dictionary<System.Type, int>();

			// Combine event list from the content types with any field not associated with a content type.
			foreach (var kvp in Database.ContentTypes.TypeMap)
			{
				var typeMeta = kvp.Value;

				if (typeMeta.Service == null)
				{
					continue;
				}

				var eventGroup = typeMeta.Service.GetEventGroup();

				if (eventGroup == null || eventGroup.All == null)
				{
					continue;
				}

				var typeName = typeMeta.Service.EntityName;

				foreach (var handler in eventGroup.All)
				{
					CreateEventDescription(elc, uniqueTypes, handler, typeName);
				}
			}

			_eventListCache = elc;
			return _eventListCache;
		}

		private void CreateEventDescription(EventList list, Dictionary<System.Type, int> uniqueTypes, EventHandler handler, string groupName)
		{
			var descrip = new EventDescription()
			{
				Name = string.IsNullOrEmpty(groupName) ? handler.Name : groupName + '.' + handler.Name
			};

			// For each type, look it up in uniqueTypes.
			// If it doesn't exist, map it in.

			var genericArgs = handler.GetType().GetGenericArguments();

			if (!uniqueTypes.TryGetValue(typeof(Context), out int contextTypeIndex))
			{
				var typeDescription = CreateTypeDescription(typeof(Context));
				contextTypeIndex = list.Types.Count;
				list.Types.Add(typeDescription);
				uniqueTypes[typeof(Context)] = contextTypeIndex;
			}

			var paramTypes = new int[genericArgs.Length + 1];
			paramTypes[0] = contextTypeIndex;
			var i = 1;

			foreach (var paramType in genericArgs)
			{
				if (!uniqueTypes.TryGetValue(paramType, out int paramTypeIndex))
				{
					var typeDescription = CreateTypeDescription(paramType);
					paramTypeIndex = list.Types.Count;
					list.Types.Add(typeDescription);
					uniqueTypes[paramType] = paramTypeIndex;
				}

				paramTypes[i++] = paramTypeIndex;
			}

			descrip.ParameterTypes = paramTypes;

			list.Results.Add(descrip);
		}

		private TypeDescription CreateTypeDescription(System.Type type)
		{
			var descrip = new TypeDescription();
			descrip.Name = CreateTypeName(type);
			descrip.Members = new List<MemberDescription>();
			return descrip;
		}

		/// <summary>
		/// Creates a name for a type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public string CreateTypeName(System.Type type)
		{
			if (type.IsArray)
			{
				return CreateTypeName(type.GetElementType()) + "[]";
			}

			// Is it a generic type?
			if (type.IsGenericType)
			{
				var genericArgs = type.GetGenericArguments();

				// Specifically List? If yes, we'll treat it like an array too.
				if (type.GetGenericTypeDefinition() == typeof(List<>))
				{
					return CreateTypeName(genericArgs[0]) + "[]";
				}

				// E.g. filter here. The result type is effectively still a unique type.

				var baseName = type.Name;
				var dashIndex = baseName.IndexOf('`');

				if (dashIndex != -1)
				{
					baseName = baseName.Substring(0, dashIndex);
				}

				var fullName = baseName + "<";

				for (var i = 0; i < genericArgs.Length; i++)
				{
					if (i != 0)
					{
						fullName += ',';
					}

					fullName += CreateTypeName(genericArgs[i]);
				}

				return fullName + ">";
			}

			if (type == typeof(bool))
			{
				return "bool";
			}
			else if (type == typeof(byte))
			{
				return "byte";
			}
			else if (type == typeof(sbyte))
			{
				return "byte";
			}
			else if (type == typeof(short))
			{
				return "short";
			}
			else if (type == typeof(ushort))
			{
				return "ushort";
			}
			else if (type == typeof(int))
			{
				return "int";
			}
			else if (type == typeof(uint))
			{
				return "uint";
			}
			else if (type == typeof(long))
			{
				return "long";
			}
			else if (type == typeof(ulong))
			{
				return "ulong";
			}
			else if (type == typeof(float))
			{
				return "float";
			}
			else if (type == typeof(double))
			{
				return "double";
			}
			else if (type == typeof(decimal))
			{
				return "decimal";
			}
			else if (type == typeof(object))
			{
				return "object";
			}
			else if (type == typeof(string))
			{
				return "string";
			}

			return type.Name;
		}
	}

	/// <summary>
	/// The list of available events.
	/// </summary>
	public class EventList
	{
		/// <summary>
		/// All the types referenced by the event descriptions.
		/// </summary>
		public List<TypeDescription> Types = new List<TypeDescription>();

		/// <summary>
		/// The complete list of events.
		/// </summary>
		public List<EventDescription> Results = new List<EventDescription>();
	}

	/// <summary>
	/// Type info.
	/// </summary>
	public class TypeDescription
	{
		/// <summary>
		/// The name of the type.
		/// </summary>
		public string Name;

		/// <summary>
		/// The field/ property info for this type.
		/// </summary>
		public List<MemberDescription> Members;
	}

	/// <summary>
	/// Field/ property info.
	/// </summary>
	public class MemberDescription
	{
		/// <summary>
		/// The name of the member.
		/// </summary>
		public string Name;

	}

	/// <summary>
	/// Description of an available event.
	/// </summary>
	public class EventDescription
	{
		/// <summary>
		/// The name of the event. Usually nested, E.g. User.AfterCreate
		/// </summary>
		public string Name;

		/// <summary>
		/// The IDs of the types used by the events parameters. Context is always present as the first one. The 2nd one is always the return type.
		/// </summary>
		public int[] ParameterTypes;
	}

}
