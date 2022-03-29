using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Lumity.BlockChains;

namespace Api.BlockDatabase;

/// <summary>
/// Handles creation of blocks.
/// </summary>
[LoadPriority(1)]
public partial class BlockDatabaseService : AutoService
{
	private BlockChain _chain;

	/// <summary>
	/// Gets the blockchain schema.
	/// </summary>
	/// <returns></returns>
	public Lumity.BlockChains.Schema GetSchema()
	{
		return _chain.Schema;
	}
	
	/// <summary>
	/// Gets the blockchain.
	/// </summary>
	/// <returns></returns>
	public BlockChain GetChain()
	{
		return _chain;
	}

	private Dictionary<ulong, BlockTableMeta> metaLookup = new Dictionary<ulong, BlockTableMeta>();

	/// <summary>
	/// Looks up the given meta type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public BlockTableMeta GetTableMeta(Type type)
	{
		foreach (var kvp in metaLookup)
		{
			if (kvp.Value.Type == type)
			{
				return kvp.Value;
			}
		}

		return null;
	}

	/// <summary>
	/// Creates the blockchain table metadata, used for converting between C# types and Lumity blockchain tx.
	/// </summary>
	/// <param name="forDefinition"></param>
	/// <param name="systemType"></param>
	/// <returns></returns>
	public BlockTableMeta CreateTableMeta(Definition forDefinition, Type systemType)
	{
		var btm = new BlockTableMeta() {
			Id = forDefinition.Id,
			Definition = forDefinition,
			Type = systemType
		};
		metaLookup[forDefinition.Id] = btm;

		return btm;
	}

	/// <summary>
	/// Writes the given object as a transaction using the given meta for the type T.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="obj"></param>
	/// <param name="meta"></param>
	public void Write<T>(T obj, BlockTableMeta meta)
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		var now = (ulong)DateTime.UtcNow.Ticks;

		// Create a buffer which will be written out repeatedly:

		// Creating an instance of the definition:
		writer.WriteInvertibleCompressed(meta.Id);

		// Field count +1 for the timestamp:
		writer.WriteInvertibleCompressed(meta.FieldCountPlusOne);

		// Timestamp:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(now);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.TimestampDefId);

		meta.WriteObject(obj, writer);

		// Field count again (for readers going backwards):
		writer.WriteInvertibleCompressed(meta.FieldCountPlusOne);

		// Definition again (for readers going backwards):
		writer.WriteInvertibleCompressed(meta.Id);

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		_chain.AddBuffers(first, last);
	}

	public T GetResult<T, ID>(
		Context context, ID id, Type instanceType, BlockTableMeta meta
	)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		T result = null;

		_chain.LoadForwards((TransactionReader reader) => {

			var defn = reader.Definition;

			if (defn == null || defn.Id != meta.Id)
			{
				return;
			}

			// Got a record we're interested in.
			// For now it's the whole thing.
			var t = (T)Activator.CreateInstance(instanceType);

			meta.ReadObject(t, reader);

			if (t.Id.Equals(id))
			{
				result = t;
				reader.Halt = true;
			}

		});

		return result;
	}

	public int GetResults<T, ID>(
		Context context, QueryPair<T, ID> queryPair, Type instanceType, BlockTableMeta meta
	)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		var index = 0;

		_chain.LoadForwards((TransactionReader reader) => {

			var defn = reader.Definition;

			if (defn == null || defn.Id != meta.Id)
			{
				return;
			}

			// Got a record we're interested in.
			// For now it's the whole thing.
			var t = (T)Activator.CreateInstance(instanceType);

			meta.ReadObject(t, reader);

			queryPair.OnResult(context, t, index, queryPair.SrcA, queryPair.SrcB);
			index++;
		});

		return index;
	}

	/// <summary>
	/// Instanced automatically. Supports dependency injection.
	/// </summary>
	public BlockDatabaseService()
	{
		// Get path to data directory:
		string dbPath = SetupDirectory();

		var chainFile = dbPath + "database.lbc";

		_chain = new BlockChain(chainFile);
		_chain.LoadOrCreate();

	}

	/// <summary>
	/// Gets a definition from system type. Cache the result when possible. Null if not found.
	/// </summary>
	/// <param name="systemType"></param>
	/// <returns></returns>
	public Definition GetDefinition(Type systemType)
	{
		var name = systemType.Name;
		return _chain.Schema.FindDefinition(name);
	}

	/// <summary>
	/// Sets up the db directory
	/// </summary>
	/// <returns></returns>
	private string SetupDirectory()
	{
		var contentPath = "Content/database/";

		contentPath = Path.GetFullPath(contentPath);

		if (!Directory.Exists(contentPath))
		{
			Directory.CreateDirectory(contentPath);
		}

		return contentPath;
	}

}


