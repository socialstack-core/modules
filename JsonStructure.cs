using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;


namespace Api.Startup
{

	/// <summary>
	/// Used when searching for a field.
	/// </summary>
	public enum JsonFieldGroup : int
	{
		/// <summary>
		/// The default group is set regardless of if the entity ID is known yet.
		/// </summary>
		Default = 1,
		/// <summary>
		/// Fields in this group are only set after an entity ID is known.
		/// </summary>
		AfterId = 2,
		/// <summary>
		/// Either the default or after ID group.
		/// </summary>
		Any = 3
	}

	/// <summary>
	/// Describes the available fields on a particular type.
	/// This exists so we can, for example, role restrict setting particular fields.
	/// </summary>
	public class JsonStructure<T> where T: DatabaseRow, new()
	{
		/// <summary>
		/// The role that this structure is for.
		/// </summary>
		public Role ForRole;
		/// <summary>
		/// All raw fields in this structure.
		/// </summary>
		public Dictionary<string, JsonField<T>> Fields;
		/// <summary>
		/// The after ID fields in this structure.
		/// </summary>
		public Dictionary<string, JsonField<T>> AfterIdFields;
		/// <summary>
		/// The before ID fields in this structure.
		/// </summary>
		public Dictionary<string, JsonField<T>> BeforeIdFields;



		/// <summary>
		/// Creates a new structure for the given role.
		/// </summary>
		/// <param name="forRole"></param>
		public JsonStructure(Role forRole)
		{
			ForRole = forRole;
			Fields = new Dictionary<string, JsonField<T>>();
			AfterIdFields = new Dictionary<string, JsonField<T>>();
			BeforeIdFields = new Dictionary<string, JsonField<T>>();
		}
		
		/// <summary>
		/// Builds this structure now. It looks at all public fields and properties of a type
		/// and for each one, triggers an event. The event can return either nothing at all - which will outright block the field - 
		/// or the event can add a special value handler which will map the raw JSON value to the actual object for us.
		/// </summary>
		public async Task Build(EventGroup<T> eventSystem)
		{
			var context = new Context();

			var fields = typeof(T).GetFields();
			
			foreach(var field in fields)
			{
				var jsonField = new JsonField<T>()
				{
					Name = field.Name,
					Structure = this,
					TargetType = field.FieldType
				};
				jsonField.FieldInfo = field;
				await TryAddField(context, jsonField, eventSystem);
			}
			
			var properties = typeof(T).GetProperties();
			
			foreach(var property in properties)
			{
				var jsonField = new JsonField<T>()
				{
					Name = property.Name,
					Structure = this,
					TargetType = property.PropertyType
				};
				
				jsonField.PropertyGet = property.GetGetMethod();
				jsonField.PropertySet = property.GetSetMethod();
				
				await TryAddField(context, jsonField, eventSystem);
			}
			
		}

		/// <summary>
		/// Adds the given field to this structure.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="field"></param>
		/// <param name="eventSystem"></param>
		private async Task TryAddField(Context context, JsonField<T> field, EventGroup<T> eventSystem)
		{
			field = await eventSystem.BeforeSettable.Dispatch(context, field);

			// If the event didn't outright block the field..
			if (field == null)
			{
				return;
			}

			var lowerName = field.Name.ToLower();

			if (field.AfterId)
			{
				AfterIdFields[lowerName] = field;
			}
			else
			{
				BeforeIdFields[lowerName] = field;
			}

			Fields[lowerName] = field;
		}

		/// <summary>
		/// Attempts to get a given case insensitive field.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="fieldGroup"></param>
		public JsonField<T> GetField(string name, JsonFieldGroup fieldGroup)
		{
			JsonField<T> result;

			switch (fieldGroup)
			{
				default:
				case JsonFieldGroup.Default:
					BeforeIdFields.TryGetValue(name.ToLower(), out result);
				break;
				case JsonFieldGroup.AfterId:
					AfterIdFields.TryGetValue(name.ToLower(), out result);
				break;
				case JsonFieldGroup.Any:
					Fields.TryGetValue(name.ToLower(), out result);
				break;
			}
			
			return result;
		}
		
	}
	
	/// <summary>
	/// A field within a JsonStructure.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class JsonField<T> where T: DatabaseRow, new(){
		
		/// <summary>
		/// The name of the field.
		/// </summary>
		public string Name;
		/// <summary>
		/// The structure this field belongs to.
		/// </summary>
		public JsonStructure<T> Structure;
		/// <summary>
		/// If this is a field, the underlying FieldInfo. Null otherwise.
		/// </summary>
		public FieldInfo FieldInfo;
		/// <summary>
		/// Set this to true if it should only be applied after an objects ID is known.
		/// </summary>
		public bool AfterId;
		/// <summary>
		/// If this is a Property, the get method. Null otherwise.
		/// </summary>
		public MethodInfo PropertyGet;
		/// <summary>
		/// If this is a Property, the set method. Null otherwise.
		/// </summary>
		public MethodInfo PropertySet;
		/// <summary>
		/// An event which is called when the value is set. It returns the value it wants to be set.
		/// </summary>
		public EventHandler<object, T, JToken> OnSetValue = new EventHandler<object, T, JToken>(null);
		/// <summary>
		/// The field/ property value type.
		/// </summary>
		public Type TargetType;

		/// <summary>
		/// The role that this is for.
		/// </summary>
		public Role ForRole
		{
			get
			{
				return Structure.ForRole;
			}
		}

		private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		private DateTime ConvertFromJsUnixTimestamp(double timestamp)
		{
			return _unixEpoch.AddSeconds(timestamp / 1000);
		}

		/// <summary>
		/// Attempts to set the value of the field.
		/// </summary>
		public async Task SetValue(Context context, T onObject, JToken value)
		{
			object targetValue = value;
			
			if(OnSetValue != null)
			{
				targetValue = await OnSetValue.Dispatch(context, targetValue, onObject, value);
			}

			// Do this rather than use value just in case the OnSetValue returned a JToken:
			var targetJToken = targetValue as JToken;

			if (targetJToken != null)
			{
				// Still a JToken - lets try and map it through now.

				if (TargetType == typeof(DateTime))
				{
					// Special case for a date. If the value is numeric, it's a JS compatible timestamp (unix timestamp in *milliseconds*).
					// Otherwise, it's the JS compatible date string.

					if (targetJToken.Type == JTokenType.Integer || targetJToken.Type == JTokenType.Float)
					{
						// JS Timestamp (milliseconds).
						var msTimestamp = targetJToken.ToObject<double>();

						targetValue = ConvertFromJsUnixTimestamp(msTimestamp);
					}
					else
					{
						if (DateTime.TryParse(
							value.ToObject<string>(),
							CultureInfo.InvariantCulture,
							System.Globalization.DateTimeStyles.RoundtripKind,
							out DateTime dateResult))
						{
							targetValue = dateResult;
						}
						else
						{
							// Unrecognised date format.
							#warning submitting an invalid date string format to the API will silently ignore the field
							return;
						}
					}

				}
				else
				{
					targetValue = targetJToken.ToObject(TargetType);
				}
			}
			
			// Note that both the setter and the FieldInfo can be null (readonly properties).
			if(PropertySet != null)
			{
				PropertySet.Invoke(onObject, new object[]{targetValue});
			}
			else if(FieldInfo != null)
			{
				FieldInfo.SetValue(onObject, targetValue);
			}
			
		}
		
	}
	
}