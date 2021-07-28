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
		public virtual void WriteJson(T obj, Writer writer)
		{

		}
	}

	/// <summary>
	/// Reads/writes in the binary bolt format.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class BoltReaderWriter<T>
		where T: new()
	{
		/// <summary>
		/// Writes the given object to the given writer in raw bolt continuous field format.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		public virtual void Write(T obj, Writer writer)
		{

		}

		/// <summary>
		/// Reads the type from the reader and removes the concrete type ref.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public object ReadGeneric(Client client)
		{
			var result = new T();
			Read(result, client);
			return result;
		}

		/// <summary>
		/// Allocates an object of the given type and reads it from the given client.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public T Read(Client client)
		{
			var result = new T();
			Read(result, client);
			return result;
		}

		/// <summary>
		/// Reads to the given object from the given writer in raw bolt continuous field format.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="reader"></param>
		public virtual void Read(T obj, Client reader)
		{

		}

		/// <summary>
		/// Field description.
		/// </summary>
		public string Description;
	}

}