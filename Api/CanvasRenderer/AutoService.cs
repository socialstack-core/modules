using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.SocketServerLibrary;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
public partial class AutoService<T, ID> : AutoService 
	where T: Content<ID>, new()
	where ID: struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	
	/// <summary>
	/// Gets objects from this service using a generic serialized filter. Use List instead whenever possible.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filterJson"></param>
	/// <param name="includes"></param>
	/// <param name="so"></param>
	/// <returns></returns>
	public override async Task ListForSSR(Context context, string filterJson, string includes, Microsoft.ClearScript.ScriptObject so)
	{
		var filterNs = Newtonsoft.Json.JsonConvert.DeserializeObject(filterJson) as JObject;

		var filter = LoadFilter(filterNs) as Filter<T, ID>;

		// Write:
		var writer = Writer.GetPooled();
		writer.Start(null);

		await ToJson(context, filter, async (Context ctx, Filter<T, ID> filt, Func<T, int, ValueTask> onResult) => {

			return await GetResults(ctx, filt, async (Context ctx2, T result, int index, object src, object src2) => {

				var _onResult = src as Func<T, int, ValueTask>;
				await _onResult(result, index);

			}, onResult, null);

		}, writer, null, includes, filter.IncludeTotal);

		filter.Release();
		var jsonResult = writer.ToUTF8String();
		writer.Release();
		so.Invoke(false, jsonResult);
	}

	/// <summary>
	/// Gets objects from this service using a generic serialized filter. Use List instead whenever possible.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filterJson"></param>
	/// <param name="includes"></param>
	/// <returns></returns>
	public override async Task<string> ListForSSR(Context context, string filterJson, string includes)
	{
		var filterNs = Newtonsoft.Json.JsonConvert.DeserializeObject(filterJson) as JObject;

		var filter = LoadFilter(filterNs) as Filter<T, ID>;

		// Write:
		var writer = Writer.GetPooled();
		writer.Start(null);

		await ToJson(context, filter, async (Context ctx, Filter<T, ID> filt, Func<T, int, ValueTask> onResult) => {

			return await GetResults(ctx, filt, async (Context ctx2, T result, int index, object src, object src2) => {

				var _onResult = src as Func<T, int, ValueTask>;
				await _onResult(result, index);

			}, onResult, null);

		}, writer, null, includes, filter.IncludeTotal);

		filter.Release();
		var jsonResult = writer.ToUTF8String();
		writer.Release();
		
		return jsonResult;
	}

	/// <summary>
	/// Gets an object from this service for use by the serverside renderer. Returns it by executing the given callback.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="includes"></param>
	public override async Task<string> GetForSSR(Context context, ulong id, string includes)
	{
		// Get the object (includes the perm checks):
		var content = await Get(context, _idConverter.Convert(id));

		// Write:
		var writer = Writer.GetPooled();
		writer.Start(null);
		await ToJson(context, content, writer, null, includes);
		var jsonResult = writer.ToUTF8String();
		writer.Release();
		return jsonResult;
	}

	/// <summary>
	/// Gets an object from this service for use by the serverside renderer. Returns it by executing the given callback.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="includes"></param>
	/// <param name="so"></param>
	public override async ValueTask GetForSSR(Context context, ulong id, string includes, Microsoft.ClearScript.ScriptObject so)
	{
		// Get the object (includes the perm checks):
		var content = await Get(context, _idConverter.Convert(id));

		// Write:
		var writer = Writer.GetPooled();
		writer.Start(null);
		await ToJson(context, content, writer, null, includes);
		var jsonResult = writer.ToUTF8String();
		writer.Release();
		so.Invoke(false, jsonResult);
	}
	
}

/// <summary>
/// The base class of all AutoService instances.
/// </summary>
public partial class AutoService
{
	/// <summary>
	/// Gets objects from this service using a generic serialized filter. Use List instead whenever possible.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filterJson"></param>
	/// <param name="includes"></param>
	/// <param name="so"></param>
	/// <returns></returns>
	public virtual Task GetForSSR(Context context, string filterJson, string includes, Microsoft.ClearScript.ScriptObject so)
	{
		return null;
	}

	/// <summary>
	/// Gets objects from this service using a generic serialized filter. Use List instead whenever possible.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filterJson"></param>
	/// <param name="includes"></param>
	/// <param name="so"></param>
	/// <returns></returns>
	public virtual Task ListForSSR(Context context, string filterJson, string includes, Microsoft.ClearScript.ScriptObject so)
	{
		return null;
	}

	/// <summary>
	/// Gets objects from this service using a generic serialized filter. Use List instead whenever possible.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filterJson"></param>
	/// <param name="includes"></param>
	/// <returns></returns>
	public virtual Task<string> ListForSSR(Context context, string filterJson, string includes)
	{
		return null;
	}

	/// <summary>
	/// Gets an object from this service for use by the serverside renderer. Returns it by executing the given callback.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="includes"></param>
	/// <param name="so"></param>
	public virtual ValueTask GetForSSR(Context context, ulong id, string includes, Microsoft.ClearScript.ScriptObject so)
	{
		return new ValueTask();
	}

	/// <summary>
	/// Gets an object from this service for use by the serverside renderer. Returns it by executing the given callback.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="includes"></param>
	public virtual Task<string> GetForSSR(Context context, ulong id, string includes)
	{
		return null;
	}
}
