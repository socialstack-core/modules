using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;


namespace Api.Startup
{
	/// <summary>
	/// A reader/ writer.
	/// </summary>
	public class TypeReaderWriter<T>
    {
		/// <summary>
		/// Writes the given object to the given writer, in binary (raw, bolt) format.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		public virtual void Write(T obj, Writer writer)
		{
			
		}

		/// <summary>
		/// Writes the given object to the given writer in JSON format.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		public virtual void WriteJson(T obj, Writer writer)
		{

		}
	}

}