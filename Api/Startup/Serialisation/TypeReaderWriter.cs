using Api.Contexts;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;


namespace Api.Startup
{
	/// <summary>
	/// Base class for JSON reader/ writers.
	/// </summary>
	public partial class TypeReaderWriter
	{
		
		/// <summary>
		/// Generic object write.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		public virtual void WriteJsonPartialObject(object obj, Writer writer)
		{
			
		}

		/// <summary>
		/// Generic object write.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		/// <param name="context"></param>
		/// <param name="flags"></param>
		public virtual void WriteJsonUnclosedObject(object obj, Writer writer, Context context, ContextFlags flags)
		{

		}

	}

	/// <summary>
	/// A reader/ writer.
	/// </summary>
	public partial class TypeReaderWriter<T> : TypeReaderWriter
	{
		/// <summary>
		/// Writes only the type and id fields of the given object.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		public virtual void WriteJsonPartial(T obj, Writer writer)
		{

		}

		/// <summary>
		/// Writes the given object to the given writer in JSON format.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		/// <param name="context"></param>
		/// <param name="flags"></param>
		public virtual void WriteJsonUnclosed(T obj, Writer writer, Context context, ContextFlags flags)
		{

		}

		/// <summary>
		/// Generic object write.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		public override void WriteJsonPartialObject(object obj, Writer writer)
		{
			WriteJsonPartial((T)obj, writer);
		}

		/// <summary>
		/// Generic object write.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		/// <param name="context"></param>
		/// <param name="flags"></param>
		public override void WriteJsonUnclosedObject(object obj, Writer writer, Context context, ContextFlags flags)
		{
			WriteJsonUnclosed((T)obj, writer, context, flags);
		}

	}

	/// <summary>
	/// Used for writing/ reading types as documents. 
	/// These write all db storable content in to a JSON object which is not wrapped in "result".
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public partial class TypeDocumentReaderWriter<T>
	{
		/// <summary>
		/// The field set as it was when this was created.
		/// </summary>
		public ContentFields Fields;

		/// <summary>
		/// Writes the given object as stored JSON.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		public virtual void WriteStoredJson(T obj, Writer writer)
		{
			
		}

		/// <summary>
		/// Reads the given JObject in to the given target object.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="src"></param>
		public virtual void ReadStoredJson(T obj, JObject src)
		{

		}

	}
}