using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Api.SocketServerLibrary;
using System.IO;


/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
public partial class AutoService<T, ID>{

	/// <summary>
	/// {"result": 
	/// </summary>
	private static readonly byte[] ResultHeader = new byte[] { (byte)'{', (byte)'"', (byte)'r', (byte)'e', (byte)'s', (byte)'u', (byte)'l', (byte)'t', (byte)'"', (byte)':' };

	/// <summary>
	/// {"total": 
	/// </summary>
	private static readonly byte[] TotalHeader = new byte[] { (byte)'{', (byte)'"', (byte)'t', (byte)'o', (byte)'t', (byte)'a', (byte)'l', (byte)'"', (byte)':' };

	/// <summary>
	/// ,"results": (comes after total)
	/// </summary>
	private static readonly byte[] ResultsHeaderAfterTotal = new byte[] {(byte)',', (byte)'"', (byte)'r', (byte)'e', (byte)'s', (byte)'u', (byte)'l', (byte)'t', (byte)'s', (byte)'"', (byte)':', (byte)'[' };

	/// <summary>
	/// {"results": (no total)
	/// </summary>
	private static readonly byte[] ResultsHeader = new byte[] { (byte)'{', (byte)'"', (byte)'r', (byte)'e', (byte)'s', (byte)'u', (byte)'l', (byte)'t', (byte)'s', (byte)'"', (byte)':', (byte)'[' };

	/// <summary>
	/// ]}
	/// </summary>
	private static readonly byte[] ResultsFooter = new byte[] { (byte)']', (byte)'}' };

	/// <summary>
	/// "null"
	/// </summary>
	private static readonly byte[] NullText = new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };

	/// <summary>
	/// ,"includes":[ 
	/// </summary>
	private static readonly byte[] IncludesHeader = new byte[] {
		(byte)',', (byte)'"', (byte)'i', (byte)'n', (byte)'c', (byte)'l', (byte)'u', (byte)'d', (byte)'e', (byte)'s', (byte)'"', (byte)':', (byte)'['
	};

	private static readonly byte[] IncludesFooter = new byte[] { (byte) ']', (byte)'}' };

	/// <summary>
	/// End of include block. ]}.
	/// </summary>
	private static readonly byte[] IncludesValueFooter = new byte[] { (byte)']', (byte)'}' };

	/// <summary>
	/// Serialises the given object into the given stream (usually a response stream). By using this method, it will consider the fields a user is permitted to see (based on the role in the context)
	/// and also may use a per-object cache which contains string segments.
	/// addResultWrap will wrap the object with {"result":...}. It is assumed true if includes is not null.
	/// </summary>
	public async ValueTask ToJson(Context context, ListWithTotal<T> entities, Stream targetStream, string includes = null)
	{
		// Get the json structure:
		var jsonStructure = await GetTypedJsonStructure(context);

		if (jsonStructure.TypeIO == null)
		{
			jsonStructure.TypeIO = TypeIOEngine.Generate(jsonStructure);
		}

		// Get the include set (can be null). Must happen first such that if it errors, nothing was written out to the stream.
		var includeSet = await GetContentFields().GetIncludeSet(includes);
		
		var writer = Writer.GetPooled();
		writer.Start(null);

		if (entities.Total.HasValue)
		{
			writer.Write(TotalHeader, 0, 9);
			writer.WriteS(entities.Total.Value);
			writer.Write(ResultsHeaderAfterTotal, 0, 12);
		}
		else
		{
			writer.Write(ResultsHeader, 0, 12);
		}

		// Obtain ID collectors, and then collect the IDs.
		var firstCollector = includeSet == null ? null : includeSet.RootInclude.GetCollectors();
		
		for (var i = 0; i < entities.Results.Count; i++)
		{
			if (i != 0)
			{
				writer.Write((byte)',');
			}

			var entity = entities.Results[i];

			if (entity == null)
			{
				writer.Write(NullText, 0, 4);
			}
			else
			{
				jsonStructure.TypeIO.WriteJson(entity, writer);

				// Collect IDs from it:
				var current = firstCollector;

				while (current != null)
				{
					current.Collect(entities.Results[i]);
					current = current.NextCollector;
				}
			}

			// Flush after each one:
			await writer.CopyToAsync(targetStream);
			writer.Reset(null);
		}

		if (includeSet == null)
		{
			writer.Write(ResultsFooter,0, 2);
			await writer.CopyToAsync(targetStream);
			writer.Release();
		}
		else
		{
			// We've got some includes to add.
			// Write the includes header, then write out the data so far.
			writer.Write((byte)']');
			writer.Write(IncludesHeader, 0, 13);
			await writer.CopyToAsync(targetStream);
			writer.Reset(null);

			// Execute all inclusions (internally releases the collectors):
			await ExecuteIncludes(context, targetStream, writer, firstCollector, includeSet.RootInclude);

			writer.Write(IncludesFooter, 0, 2);

			// Copy remaining bits:
			await writer.CopyToAsync(targetStream);

			// Release writer when fully done:
			writer.Release();
		}
	}


	/// <summary>
	/// Serialises the given object into the given stream (usually a response stream). By using this method, it will consider the fields a user is permitted to see (based on the role in the context)
	/// and also may use a per-object cache which contains string segments.
	/// addResultWrap will wrap the object with {"result":...}. It is assumed true if includes is not null.
	/// </summary>
	public async ValueTask ToJson(Context context, T entity, Stream targetStream, string includes = null)
	{
		// Get the json structure:
		var jsonStructure = await GetTypedJsonStructure(context);

		if (jsonStructure.TypeIO == null)
		{
			jsonStructure.TypeIO = TypeIOEngine.Generate(jsonStructure);
		}

		// Get the include set (can be null):
		var includeSet = await GetContentFields().GetIncludeSet(includes);
		
		var writer = Writer.GetPooled();
		writer.Start(null);

		writer.Write(ResultHeader, 0, 10);

		if (entity == null)
		{
			writer.Write(NullText, 0, 4);
		}
		else
		{
			jsonStructure.TypeIO.WriteJson(entity, writer);
		}

		if (includeSet == null)
		{
			writer.Write((byte)'}');
			await writer.CopyToAsync(targetStream);
			writer.Release();
		}
		else
		{
			// We've got some includes to add.
			// Write the includes header, then write out the data so far.
			writer.Write(IncludesHeader, 0, 13);
			await writer.CopyToAsync(targetStream);
			writer.Reset(null);

			// First we need to obtain ID collectors, and then collect the IDs.
			var firstCollector = includeSet.RootInclude.GetCollectors();

			// Collect all IDs:
			if (entity != null)
			{
				var current = firstCollector;

				while (current != null)
				{
					current.Collect(entity);
					current = current.NextCollector;
				}

				await ExecuteIncludes(context, targetStream, writer, firstCollector, includeSet.RootInclude);
			}

			writer.Write(IncludesFooter, 0, 2);

			// Copy remaining bits:
			await writer.CopyToAsync(targetStream);

			// Release writer when fully done:
			writer.Release();
		}
	}

	private async ValueTask ExecuteIncludes(Context context, Stream targetStream, Writer writer, IDCollector firstCollector, InclusionNode includeNode)
	{
		// Now all IDs that are needed have been collected,
		// go through the inclusions and perform the include.
		var includesToExecute = includeNode.ChildNodes;

		for (var i = 0; i < includesToExecute.Length; i++)
		{
			var toExecute = includesToExecute[i];

			if (toExecute.InclusionOutputIndex != 0)
			{
				// Comma between includes. Exists for all nodes except the very first include (output index 0).
				writer.Write((byte)',');
			}

			// Write the inclusion node header:
			var h = toExecute.IncludeHeader;
			writer.Write(h, 0, h.Length);

			// Get ID collector:
			var collector = firstCollector;
			var curIndex = 0;

			// A linked list is by far the best structure here - the set is usually tiny and it avoids allocating.
			while (curIndex < toExecute.CollectorIndex)
			{
				curIndex++;
				collector = collector.NextCollector;
			}

			// Spawn child collectors now, if we need any.
			var childCollectors = toExecute.GetCollectors();

			if (toExecute.MappingTargetField == null)
			{
				// Directly use IDs in collector with the service.
				await toExecute.Service.OutputJsonList(context, childCollectors, collector, writer); // Calls OutputJsonList on the service
			}
			else
			{
				// Write out a mapping of src->target IDs.
				var mappingCollector = toExecute.MappingTargetField.RentCollector();

				// Output the mapping array, whilst also collecting the IDs of the things it is mapping to.
				await toExecute.MappingService.OutputMap(context, mappingCollector, collector, writer);

				// Write out the mapping data to the target stream:
				await writer.CopyToAsync(targetStream);
				writer.Reset(null);

				// Write the values:
				await toExecute.Service.OutputJsonList(context, childCollectors, mappingCollector, writer); // Calls OutputJsonList on the service

				// Release mapping collector:
				mappingCollector.Release();
			}

			// End of this include.
			writer.Write(IncludesValueFooter, 0, 2);

			// Did it have any child nodes? If so, execute those as well.
			// Above we will have collected the IDs that the children need.
			if (toExecute.ChildNodes != null && toExecute.ChildNodes.Length > 0)
			{
				// NB: This will release the child collectors for us.
				await ExecuteIncludes(context, targetStream, writer, childCollectors, toExecute);
			}
		}

		// Release the collectors:
		var current = firstCollector;

		while (current != null)
		{
			var next = current.NextCollector;
			current.Release();
			current = next;
		}
	}

	/// <summary>
	/// Serialises the given object into the given stream (usually a response stream). By using this method, it will consider the fields a user is permitted to see (based on the role in the context)
	/// and also may use a per-object cache which contains string segments.
	/// addResultWrap will wrap the object with {"result":...}. It is assumed true if includes is not null.
	/// </summary>
	public async ValueTask ToJson(Context context, T entity, Writer writer, InclusionNode node)
	{
		// Get the json structure:
		var jsonStructure = await GetTypedJsonStructure(context);

		if (jsonStructure.TypeIO == null)
		{
			jsonStructure.TypeIO = TypeIOEngine.Generate(jsonStructure);
		}

		// Write the object out:
		if (entity == null)
		{
			writer.Write(NullText, 0, 4);
		}
		else
		{
			jsonStructure.TypeIO.WriteJson(entity, writer);
		}
	}

	/// <summary>
	/// Short ref to the primary index.
	/// </summary>
	private Dictionary<ID, T> _primaryIndexRef;

	/// <summary>
	/// Outputs a single object from this service as JSON into the given writer. Acts like include * was specified.
	/// Executes the given collector(s) whilst it happens, which can also be null.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public override async ValueTask OutputById(Context context, uint id, Writer writer)
	{
		// Get the object:
		var content = await _getWithIntId(context, id, DataOptions.Default);

		// Output it:
		await ToJson(context, content, writer);
	}
	
	/// <summary>
	/// Outputs a list of things from this service as JSON into the given writer.
	/// Executes the given collector(s) whilst it happens, which can also be null.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="collectors"></param>
	/// <param name="idSet"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public override async ValueTask OutputJsonList(Context context, IDCollector collectors, IDCollector idSet, Writer writer)
	{
		var collectedIds = idSet as IDCollector<ID>;

		// Get its locale 0 cache (it's a mapping type, so it's never localised):
		if (_cache != null && _primaryIndexRef == null)
		{

			var cache = GetCacheForLocale(0);

			if (cache != null)
			{
				// It's a cached mapping type.
				// Pre-obtain index ref now:
				_primaryIndexRef = cache.GetPrimary();
			}
		}

		// If cached, directly enumerate over the IDs via the cache.
		if (_primaryIndexRef != null)
		{
			// This mapping type is cached.
			var _enum = collectedIds.GetEnumerator();

			var first = true;

			while (_enum.HasMore())
			{
				// Get current value:
				var valID = _enum.Current();

				// Read that ID set from the cache:
				if (_primaryIndexRef.TryGetValue(valID, out T entity))
				{
					if (first)
					{
						first = false;
					}
					else
					{
						writer.Write((byte)',');
					}
					
					// Output the object:
					await ToJson(context, entity, writer);

					// Collect:
					var col = collectors;

					while (col != null)
					{
						col.Collect(entity);
						col = col.NextCollector;
					}
				}
			}

		}
		else if(collectedIds.Count > 0)
		{
			// DB hit. Allocate list of IDs as well.
			var idList = new List<ID>(collectedIds.Count);

			var _enum = collectedIds.GetEnumerator();

			while (_enum.HasMore())
			{
				// Get current value:
				idList.Add(_enum.Current());
			}

			var entries = await List(context, new Filter<T>().Id(idList));

			var first = true;

			foreach (var entity in entries)
			{
				// Output src, target
				if (first)
				{
					first = false;
				}
				else
				{
					writer.Write((byte)',');
				}

				// Output the object:
				await ToJson(context, entity, writer);

				// Collect:
				var col = collectors;

				while (col != null)
				{
					col.Collect(entity);
					col = col.NextCollector;
				}
			}
		}
	}

	/// <summary>
	/// Serialises the given object into the given writer. By using this method, it will consider the fields a user is permitted to see (based on the role in the context)
	/// and also may use a per-object cache which contains string segments.
	/// </summary>
	public async ValueTask ToJson(Context context, T entity, Writer writer)
	{
		// Get the json structure:
		var jsonStructure = await GetTypedJsonStructure(context);

		if (jsonStructure.TypeIO == null)
		{
			jsonStructure.TypeIO = TypeIOEngine.Generate(jsonStructure);
		}

		if (entity == null)
		{
			writer.Write(NullText, 0, 4);
		}
		else
		{
			jsonStructure.TypeIO.WriteJson(entity, writer);
		}
	}

	/*
	/// <summary>
	/// Serialises the given object into the given writer. By using this method, it will consider the fields a user is permitted to see (based on the role in the context)
	/// and also may use a per-object cache which contains string segments.
	/// </summary>
	public async ValueTask ToBolt(Context context, T entity, Writer writer)
	{
		
	}
	*/
}
