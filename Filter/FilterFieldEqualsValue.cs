using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Users;


namespace Api.Permissions
{

	/// <summary>
	/// A filter method which is active if a field of the current user matches a field on the entity.
	/// </summary>
	public partial class FilterFieldEqualsValue : FilterFieldEquals
	{
		private static object _lock = new object();
		/// <summary>
		/// Param ID which counts up to give each one a unique ref.
		/// </summary>
		public static long NextParamId = 1;

		/// <summary>
		/// A unique parameter ID.
		/// </summary>
		public long ParamId;

		/// <summary>
		/// The method to run to obtain a value to compare the field to.
		/// </summary>
		public Func<Context, Task<object>> Method;

		/// <summary>
		/// Create a new ifUserField node.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="field"></param>
		/// <param name="valueMethod"></param>
		/// <param name="paramId"></param>
		public FilterFieldEqualsValue(Type type, string field, Func<Context, Task<object>> valueMethod, long paramId = -1) : base(type, field) {

			if (paramId == -1)
			{
				lock (_lock)
				{
					ParamId = NextParamId++;
				}
			}
			else
			{
				ParamId = paramId;
			}
			Method = valueMethod;
		}

		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override async Task<bool> IsGranted(Capability capability, Context token, object[] extraObjectsToCheck)
		{
			// Get first extra arg
			if (extraObjectsToCheck == null || extraObjectsToCheck.Length < ArgIndex)
			{
				// Arg not provided. Hard fail scenario.
				return EqualsFail(capability);
			}

			var firstArg = extraObjectsToCheck[ArgIndex];
			
			if(firstArg == null)
			{
				// Required.
				return false;
			}
			
			// Is it the correct type?
			if (firstArg.GetType() != Type)
			{
				return false;
			}

			var fieldValue = FieldInfo.GetValue(firstArg);

			// Does it match?
			var compareTo = await Method(token);

			return compareTo.Equals(fieldValue);
		}

		/// <summary>
		/// True if this filter node is active on the given object.
		/// </summary>
		public override bool Matches(List<ResolvedValue> values, object obj)
		{
			if (obj == null || values == null)
			{
				return false;
			}

			// Read the value:
			var val = FieldInfo.GetValue(obj);
			
			// Get the resolved value:
			ResolvedValue rv = null;
			
			for(var i=0;i<values.Count;i++){
				if(values[i].Node == this)
				{
					rv = values[i];
					break;
				}
			}
			
			if(rv == null)
			{
				return false;
			}
			
			// Compare rv.Value to val.
			object compareWith = rv.Value;
			
			if(val == null)
			{
				return (compareWith == null);
			}
			else if(compareWith == null)
			{
				return false;
			}
			
			return val.Equals(compareWith);
		}

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterFieldEqualsValue(Type, Field, Method, ParamId);
		}
	}

	public partial class Filter
	{
		/// <summary>
		/// True if the given field (on an object of the given type) has the value returned by the given method.
		/// </summary>
		/// <returns></returns>
		public Filter FieldEqualsValue(Type type, string field, Func<Context, Task<object>> valueMethod)
		{
			var node = new FilterFieldEqualsValue(type, field, valueMethod);
			return Add(node);
		}

	}

	/// <summary>
	/// A resolved value from a resolvevr node.
	/// </summary>
	public class ResolvedValue
	{
		/// <summary>
		/// The node it came from.
		/// </summary>
		public FilterFieldEqualsValue Node;

		/// <summary>
		/// The resolved value.
		/// </summary>
		public object Value;
	}

}