using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Api.Permissions{
	
	/// <summary>
	/// Helper for generating IN(..) strings from a wide variety of different IEnumerables, directly into a Writer.
	/// </summary>
	public class InStringGenerator<T> : InStringGenerator
	{
		/// <summary>
		/// Used to output null for nullable iterators.
		/// </summary>
		protected static readonly byte[] NULL = new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
	}

	/// <summary>
	/// Helper for generating IN(..) strings from a wide variety of different IEnumerables, directly into a Writer.
	/// </summary>
	public class InStringGenerator {

		private static Dictionary<Type, InStringGenerator> _map;

		private static void BuildMap()
		{
			var allTypes = typeof(InStringGenerator).Assembly.DefinedTypes;
			var map = new Dictionary<Type, InStringGenerator>();

			foreach (var typeInfo in allTypes)
			{
				// If it:
				// - Is a class
				// - Inherits InStringGenerator
				// Then we instance it.

				if (!typeInfo.IsClass || typeInfo.BaseType == null || typeInfo.BaseType.BaseType != typeof(InStringGenerator))
				{
					continue;
				}

				var forBaseType = typeInfo.BaseType.GetGenericArguments()[0];

				// Ger the IEnumerable<> type:
				var enumType = typeof(IEnumerable<>).MakeGenericType(new Type[] { forBaseType  });

				// Got one - instance it now:
				var gen = Activator.CreateInstance(typeInfo) as InStringGenerator;
				map[enumType] = gen;

				if (forBaseType.IsValueType && Nullable.GetUnderlyingType(forBaseType) == null)
				{
					// IDCollector is only available for value types - not nullables or e.g. string.
					var collectorType = typeof(IDCollector<>).MakeGenericType(new Type[] { forBaseType });

					map[collectorType] = gen;
				}
			}

			_map = map;
		}

		/// <summary>
		/// Gets a generator for the given IEnumerable or IDCollector type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static InStringGenerator Get(Type type)
		{
			if (_map == null)
			{
				BuildMap();
			}

			_map.TryGetValue(type, out InStringGenerator result);
			return result;
		}
		
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public virtual bool Generate(Writer writer, object vals)
		{
			return false;
		}
		
		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public virtual bool GenerateFromCollector(Writer writer, object vals)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorString:InStringGenerator<string>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<string>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteEscaped(v);
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorDouble:InStringGenerator<double>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<double>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<double>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while(it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorFloat:InStringGenerator<float>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<float>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<float>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorDecimal:InStringGenerator<decimal>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<decimal>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<decimal>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorBool:InStringGenerator<bool>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<bool>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.Write(v ? (byte)'1' : '0');
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<bool>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.Write(it.Current() ? (byte)'1' : '0');
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorULong:InStringGenerator<ulong>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<ulong>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<ulong>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorLong:InStringGenerator<long>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<long>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<long>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorUInt:InStringGenerator<uint>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<uint>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<uint>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorInt:InStringGenerator<int>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<int>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<int>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorUShort:InStringGenerator<ushort>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<ushort>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<ushort>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorShort:InStringGenerator<short>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<short>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<short>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorByte:InStringGenerator<byte>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<byte>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<byte>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorSByte:InStringGenerator<sbyte>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<sbyte>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				writer.WriteS(v);
			}
			return !first;
		}

		/// <summary>
		/// Generate the series of values for the given collector.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool GenerateFromCollector(Writer writer, object vals)
		{
			var values = (IDCollector<sbyte>)vals;
			bool first = true;
			var it = values.GetNonAllocEnumerator();
			while (it.HasMore())
			{
				if (first) { first = false; } else { writer.Write((byte)','); }
				writer.WriteS(it.Current());
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorDoubleNullable:InStringGenerator<double?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<double?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorFloatNullable:InStringGenerator<float?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<float?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorDecimalNullable:InStringGenerator<decimal?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<decimal?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorBoolNullable:InStringGenerator<bool?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<bool?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.Write(v.Value ? (byte)'1' : '0');
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorULongNullable:InStringGenerator<ulong?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<ulong?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorLongNullable:InStringGenerator<long?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<long?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorUIntNullable:InStringGenerator<uint?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<uint?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorIntNullable:InStringGenerator<int?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<int?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorUShortNullable:InStringGenerator<ushort?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<ushort?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorShortNullable:InStringGenerator<short?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<short?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}

	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorByteNullable:InStringGenerator<byte?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<byte?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}
	
	/// <summary>
	/// An In(..) generator for a particular type of IEnumerable. 
	/// Use InStringGenerator.Get() instead of constructing these.
	/// </summary>
	public class InStringGeneratorSByteNullable:InStringGenerator<sbyte?>
	{
		/// <summary>
		/// Generate the series of values for the given enumerable.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vals"></param>
		public override bool Generate(Writer writer, object vals)
		{
			var values = (IEnumerable<sbyte?>)vals;
			bool first = true;
			foreach(var v in values){
				if(first){first=false;}else{writer.Write((byte)',');}
				if (v.HasValue)
				{
					writer.WriteS(v.Value);
				}
				else
				{
					writer.Write(NULL, 0, 4);
				}
			}
			return !first;
		}
	}
}