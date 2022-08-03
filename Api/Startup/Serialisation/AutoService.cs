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
using System.Collections.Concurrent;

/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
public partial class AutoService<T, ID>
{

    /// <summary>
    /// {"result": 
    /// </summary>
    private static readonly byte[] ResultHeader = new byte[] { (byte)'{', (byte)'"', (byte)'r', (byte)'e', (byte)'s', (byte)'u', (byte)'l', (byte)'t', (byte)'"', (byte)':' };

    /// <summary>
    /// {"total": 
    /// </summary>
    private static readonly byte[] TotalHeader = new byte[] { (byte)']', (byte)',', (byte)'"', (byte)'t', (byte)'o', (byte)'t', (byte)'a', (byte)'l', (byte)'"', (byte)':' };

    /// <summary>
    /// ,"results": (comes after total)
    /// </summary>
    private static readonly byte[] ResultsHeaderAfterTotal = new byte[] { (byte)',', (byte)'"', (byte)'r', (byte)'e', (byte)'s', (byte)'u', (byte)'l', (byte)'t', (byte)'s', (byte)'"', (byte)':', (byte)'[' };

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

    private static readonly byte[] IncludesFooter = new byte[] { (byte)']', (byte)'}' };

    /// <summary>
    /// End of include block. ]}.
    /// </summary>
    private static readonly byte[] IncludesValueFooter = new byte[] { (byte)']', (byte)'}' };

    /// <summary>
    /// Serialises the given object into the given stream (usually a response stream). By using this method, it will consider the fields a user is permitted to see (based on the role in the context)
    /// and also may use a per-object cache which contains string segments.
    /// dataSource is often a filter.
    /// </summary>
    public async ValueTask ToJson<ANY>(
        Context context, ANY dataSource,
        Func<Context, ANY, Func<T, int, ValueTask>, ValueTask<int>> onGetData,
        Writer writer,
        Stream targetStream = null, string includes = null, bool includeTotal = false)
    {
        // Get the json structure:
        var jsonStructure = await GetTypedJsonStructure(context);

        if (jsonStructure.TypeIO == null)
        {
            jsonStructure.TypeIO = TypeIOEngine.Generate(jsonStructure);
        }

        // Get the include set (can be null). Must happen first such that if it errors, nothing was written out to the stream.
        var includeSet = await GetContentFields().GetIncludeSet(includes);

        writer.Write(ResultsHeader, 0, 12);

        // Obtain ID collectors, and then collect the IDs.
        var firstCollector = includeSet == null ? null : includeSet.RootInclude.GetCollectors();

        var total = await onGetData(context, dataSource, async (T entity, int index) =>
        {
            if (index != 0)
            {
                writer.Write((byte)',');
            }

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
                    current.Collect(entity);
                    current = current.NextCollector;
                }
            }

            if (targetStream != null)
            {
                // Flush after each one:
                await writer.CopyToAsync(targetStream);
                writer.Reset(null);
            }
        });

        if (includeSet == null)
        {
            if (includeTotal)
            {
                writer.Write(TotalHeader, 0, 10);
                writer.WriteS(total);
                writer.Write((byte)'}');
            }
            else
            {
                writer.Write(ResultsFooter, 0, 2);
            }

            if (targetStream != null)
            {
                await writer.CopyToAsync(targetStream);
                writer.Reset(null);
            }
        }
        else
        {
            // We've got some includes to add.
            // Write the includes header, then write out the data so far.

            if (includeTotal)
            {
                writer.Write(TotalHeader, 0, 10);
                writer.WriteS(total);
            }
            else
            {
                writer.Write((byte)']');
            }

            // Starts with a ,
            writer.Write(IncludesHeader, 0, 13);

            if (targetStream != null)
            {
                await writer.CopyToAsync(targetStream);
                writer.Reset(null);
            }

            // Execute all inclusions (internally releases the collectors):
            await ExecuteIncludes(context, targetStream, writer, firstCollector, includeSet.RootInclude);

            writer.Write(IncludesFooter, 0, 2);

            if (targetStream != null)
            {
                // Copy remaining bits:
                await writer.CopyToAsync(targetStream);
                writer.Reset(null);
            }
        }
    }

    /// <summary>
    /// Outputs the given object (an entity from this service) to JSON in the given writer.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="entity"></param>
    /// <param name="writer"></param>
    /// <param name="targetStream"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public override ValueTask ObjectToJson(Context context, object entity, Writer writer, Stream targetStream = null, string includes = null)
    {
        return ToJson(context, (T)entity, writer, targetStream, includes);
    }

    /// <summary>
    /// Outputs the given object (an entity from this service) to JSON in the given writer.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="entity"></param>
    /// <param name="writer"></param>
    /// <returns></returns>
    public override async ValueTask ObjectToTypeAndIdJson(Context context, object entity, Writer writer)
    {
        // Get the json structure:
        var jsonStructure = await GetTypedJsonStructure(context);

        if (jsonStructure.TypeIO == null)
        {
            jsonStructure.TypeIO = TypeIOEngine.Generate(jsonStructure);
        }

        jsonStructure.TypeIO.WriteJsonPartial((T)entity, writer);
    }

    /// <summary>
    /// Serialises the given object into the given stream (usually a response stream). By using this method, it will consider the fields a user is permitted to see (based on the role in the context)
    /// and also may use a per-object cache which contains string segments.
    /// addResultWrap will wrap the object with {"result":...}. It is assumed true if includes is not null.
    /// </summary>
    public async ValueTask ToJson(Context context, T entity, Writer writer, Stream targetStream = null, string includes = null)
    {
        // Get the json structure:
        var jsonStructure = await GetTypedJsonStructure(context);

        if (jsonStructure.TypeIO == null)
        {
            jsonStructure.TypeIO = TypeIOEngine.Generate(jsonStructure);
        }

        // Get the include set (can be null):
        var includeSet = await GetContentFields().GetIncludeSet(includes);

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

            if (targetStream != null)
            {
                await writer.CopyToAsync(targetStream);
                writer.Reset(null);
            }
        }
        else
        {
            // We've got some includes to add.
            // Write the includes header, then write out the data so far.
            writer.Write(IncludesHeader, 0, 13);

            if (targetStream != null)
            {
                await writer.CopyToAsync(targetStream);
                writer.Reset(null);
            }

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

            if (targetStream != null)
            {
                // Copy remaining bits:
                await writer.CopyToAsync(targetStream);
                writer.Reset(null);
            }
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
                await toExecute.Service.OutputJsonList(context, childCollectors, collector, writer, true); // Calls OutputJsonList on the service
            }
            else if (toExecute.MappingService == null)
            {
                // Write the values:
                await toExecute.Service.OutputJsonList<ID>(context, childCollectors, collector, toExecute.MappingTargetFieldName, writer, true); // Calls OutputJsonList on the service
            }
            else
            {
                // Write out a mapping of src->target IDs.
                var mappingCollector = toExecute.MappingTargetField.RentCollector();

                // Output the mapping array, whilst also collecting the IDs of the things it is mapping to.
                await toExecute.MappingService.OutputMap(context, mappingCollector, collector, writer);

                if (targetStream != null)
                {
                    // Write out the mapping data to the target stream:
                    await writer.CopyToAsync(targetStream);
                    writer.Reset(null);
                }

                // Write the values:
                await toExecute.Service.OutputJsonList(context, childCollectors, mappingCollector, writer, true); // Calls OutputJsonList on the service

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
    /// Outputs a single object from this service as JSON into the given writer. Acts like include * was specified.
    /// Executes the given collector(s) whilst it happens, which can also be null.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    /// <param name="writer"></param>
    /// <returns></returns>
    public override async ValueTask OutputById(Context context, ulong id, Writer writer)
    {
        // Get the object:
        var content = await Get(context, ConvertId(id), DataOptions.Default);

        // Output it:
        await ToJson(context, content, writer, null, "*");
    }

    /// <summary>
    /// Outputs a list of things from this service as JSON into the given writer.
    /// Executes the given collector(s) whilst it happens, which can also be null.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="collectors"></param>
    /// <param name="idSet"></param>
    /// <param name="setField"></param>
    /// <param name="writer"></param>
    /// <param name="viaIncludes"></param>
    /// <returns></returns>
    public override async ValueTask OutputJsonList<S_ID>(Context context, IDCollector collectors, IDCollector idSet, string setField, Writer writer, bool viaIncludes)
    {
        var collectedIds = idSet as IDCollector<S_ID>;

        // If cached, directly enumerate over the IDs via the cache.
        if (CacheAvailable)
        {
            var cache = GetCacheForLocale(1);
            var indexRef = cache.GetIndex<S_ID>(setField) as NonUniqueIndex<T, S_ID>;

            // This mapping type is cached.
            var _enum = collectedIds.GetNonAllocEnumerator();

            var first = true;

            while (_enum.HasMore())
            {
                // Get current value:
                var valID = _enum.Current();

                // Read that ID set from the cache:
                var indexEnum = indexRef.GetEnumeratorFor(valID);

                while (indexEnum.HasMore())
                {
                    var entity = indexEnum.Current();

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
        else if (collectedIds.Count > 0)
        {
            // DB hit.
            var f = Where(setField + "=[?]")
            .Bind(collectedIds);
            f.IsIncluded = viaIncludes;

            await f
            .ListAll(
                context,
                async (Context ctx, T entity, int index, object src, object src2) =>
                {

                    // Passing these in avoids a delegate frame allocation.
                    // The casts are free because they're reference types.
                    var _writer = (Writer)src;
                    var _col = (IDCollector)src2;

                    if (index != 0)
                    {
                        _writer.Write((byte)',');
                    }

                    // Output the object:
                    await ToJson(context, entity, _writer);

                    // Collect:
                    var col = _col;

                    while (col != null)
                    {
                        col.Collect(entity);
                        col = col.NextCollector;
                    }

                },
                writer,
                collectors
            );
        }
    }

    /// <summary>
    /// Outputs a list of things from this service as JSON into the given writer.
    /// Executes the given collector(s) whilst it happens, which can also be null.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="collectors"></param>
    /// <param name="idSet"></param>
    /// <param name="writer"></param>
    /// <param name="viaIncludes"></param>
    /// <returns></returns>
    public override async ValueTask OutputJsonList(Context context, IDCollector collectors, IDCollector idSet, Writer writer, bool viaIncludes)
    {
        var collectedIds = idSet as IDCollector<ID>;

        var cache = GetCacheForLocale(context.LocaleId);
        
        // If cached, directly enumerate over the IDs via the cache.
        if (cache != null)
        {
            var primary = cache.GetPrimary();

            // This mapping type is cached.
            var _enum = collectedIds.GetNonAllocEnumerator();
            var first = true;
            while (_enum.HasMore())
            {
                // Get current value:
                var valID = _enum.Current();
                // Read that ID set from the cache:
                if (primary.TryGetValue(valID, out T entity))
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
        else if (collectedIds.Count > 0)
        {
            // DB hit.
            var f = Where("Id=[?]", DataOptions.IgnorePermissions)
            .Bind(collectedIds);
            f.IsIncluded = viaIncludes;

            await f.ListAll(
                context,
                async (Context ctx, T entity, int index, object src, object src2) =>
                {

                    // Passing these in avoids a delegate frame allocation.
                    // The casts are free because they're reference types.
                    var _writer = (Writer)src;
                    var _cols = (IDCollector)src2;

                    if (index != 0)
                    {
                        _writer.Write((byte)',');
                    }

                    // Output the object:
                    await ToJson(context, entity, _writer);

                    // Collect:
                    var col = _cols;

                    while (col != null)
                    {
                        col.Collect(entity);
                        col = col.NextCollector;
                    }
                },
                writer,
                collectors
            );
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
