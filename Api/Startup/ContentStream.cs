using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.SocketServerLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Api.Startup;

/// <summary>
/// A source for a content stream.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="ID"></typeparam>
public interface ContentStreamSource<T, ID>
where T: Content<ID>, new()
where ID : struct, IEquatable<ID>, IComparable<ID>, IConvertible
{
	/// <summary>
	/// Starts cycling results for the given filter with the given callback function. Usually use Where and then one if its convenience functions instead.
	/// </summary>
	ValueTask<int> GetResults(Context context, Filter<T, ID> filter, Func<Context, T, int, object, object, ValueTask> onResult, object srcA, object srcB);

	/// <summary>
	/// If this content stream has more than one source, this returns the next one.
	/// </summary>
	/// <returns></returns>
	SecondaryContentStreamSource GetNextSource();
}

/// <summary>
/// A secondary result set content stream source. Forms a linked list.
/// Unlike the primary result set, this one can also return any type - not just database ones.
/// These types are still permitted to use the includes system via HasVirtualField.
/// </summary>
public class SecondaryContentStreamSource
{
	/// <summary>
	/// The name that will appear in the JSON.
	/// </summary>
	public string SourceName;

	/// <summary>
	/// The next one in a linked list if there is one.
	/// </summary>
	public SecondaryContentStreamSource Next;

	/// <summary>
	/// Starts streaming the results, invoking the given callback on each one. 
	/// Unlike the primary content stream, secondary ones cannot be filtered.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="onResult"></param>
	/// <param name="srcA"></param>
	/// <param name="srcB"></param>
	/// <returns></returns>
	public virtual ValueTask<int> GetResults(Context context, Func<Context, object, int, object, object, ValueTask> onResult, object srcA, object srcB)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Gets the serializer for the type of content in this stream source.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public virtual ValueTask<TypeReaderWriter> GetSerializer(Context context)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Writes the source out as JSON to the given writer.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="writer"></param>
	/// <param name="stream"></param>
	/// <param name="includes"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public async ValueTask ToJson(Context context, Writer writer, Stream stream, IncludeSet includes, ContextFlags flags = ContextFlags.None)
	{
		writer.WriteASCII("{\"results\":[");

		var serializer = await GetSerializer(context);

		InclusionNode includeNode = null;
		IDCollector firstCollector = null;

		if (includes != null)
		{
			includeNode = includes.GetSecondaryNode(SourceName);

			if (includeNode != null)
			{
				firstCollector = includeNode.GetCollectors();

				// Functional includes are not supported on the first layer of
				// secondary sets as they can be non-content types.
			}
		}

		await GetResults(context, (Context ctx, object result, int index, object a, object b) => {

			// avoids delegate alloc
			Writer wr = (Writer)a;

			if (index != 0)
			{
				wr.Write((byte)',');
			}

			if (result == null)
			{
				wr.WriteASCII("null");
			}
			else
			{
				serializer.WriteJsonUnclosedObject(result, wr, ctx, flags);

				// Collect IDs from it:
				var current = firstCollector;

				while (current != null)
				{
					current.WriteAndCollect(context, writer, result);
					current = current.NextCollector;
				}

				wr.Write((byte)'}');
			}

			return new ValueTask();
		}, writer, null);

		if (stream != null)
		{
			await writer.CopyToAsync(stream);
			writer.Reset(null);
		}

		writer.WriteASCII("],\"includes\":[");

		if (includeNode != null)
		{
			// Execute all inclusions (internally releases the collectors):
			await includeNode.ExecuteIncludes(context, stream, writer, firstCollector, flags);
		}

		writer.WriteASCII("]}");
	}

}

/// <summary>
/// A list of objects in the API. This is only used for representation of the 
/// result set format: it is not actually instanced.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiList<T> {
	/// <summary>
	/// The result set.
	/// </summary>
	public List<T> Results { get; set; }

	/// <summary>
	/// Secondary result sets. It is effectively a dictionary of ApiLists, where each one has its own results/ includes.
	/// </summary>
	public Dictionary<string, object> Secondary { get; set; }
}

/// <summary>
/// A singular object from the API. This is only used for representation of the 
/// result format: it is not actually instanced.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiContent<T> {
	/// <summary>
	/// The singular result.
	/// </summary>
	public T Result { get; set; }
}

/// <summary>
/// Streams content from a service in a non-allocating way.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="ID"></typeparam>
public struct ContentStream<T, ID>
	where T: Content<ID>, new()
	where ID : struct, IEquatable<ID>, IComparable<ID>, IConvertible
{
	/// <summary>
	/// The source service.
	/// </summary>
	public ContentStreamSource<T, ID> Source;

	/// <summary>
	/// A filter to use.
	/// </summary>
	public Filter<T, ID> Filter;

	/// <summary>
	/// The autoservice for type T. Frequently == Source.
	/// </summary>
	public AutoService<T, ID> ServiceForType;

	/// <summary>
	/// True if the filter must be released after streaming. Happens internally.
	/// </summary>
	public bool ReleaseFilter;

	/// <summary>
	/// True if the total should be included.
	/// </summary>
	public bool IncludeTotal = false;
	
	/// <summary>
	/// A default content stream.
	/// </summary>
	public ContentStream()
	{
		
	}

	/// <summary>
	/// Convenience wrapper for creating a stream from a list.
	/// Not really ideal because the goal of a content stream is to avoid allocation of 
	/// lists, but there are plenty of times when a quick list is perfectly fine.
	/// </summary>
	/// <param name="list"></param>
	/// <param name="svc">Provide the AutoService for type T.</param>
	/// <param name="total">If there are more results (e.g. this is just one page of them), optionally provide the total result count. Otherwise the length of the list is used.</param>
	public ContentStream(List<T> list, AutoService<T, ID> svc, int? total = null)
	{
		Source = new ListStreamSource<T, ID>(list, total);
		ServiceForType = svc;
	}

	/// <summary>
	/// Starts streaming the results, invoking the given callback on each run.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="onResult"></param>
	/// <param name="srcA">An object that you can pass through to the callback without needing to heap allocate a delegate state.</param>
	/// <param name="srcB">A second object that you can pass through to the callback without needing to heap allocate a delegate state.</param>
	/// <returns></returns>
	public async ValueTask Stream(Context context, Func<Context, T, int, object, object, ValueTask> onResult, object srcA, object srcB)
	{
		await Source.GetResults(context, Filter, onResult, srcA, srcB);
		if (ReleaseFilter && Filter != null)
		{
			Filter.Release();
		}
	}

	/// <summary>
	/// Writes this content stream to a given actual stream as JSON.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="stream"></param>
	/// <param name="includes"></param>
	/// <returns></returns>
	public async ValueTask WriteJson(Context context, System.IO.Stream stream, string includes)
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		ContentStreamSource<T, ID> src = Source;
		var currentSecondarySource = src.GetNextSource();

		await ServiceForType.ToJson(context, Filter, async (Context ctx, Filter<T, ID> filt, Func<T, int, ValueTask> onResult) => {

			return await src.GetResults(ctx, filt, async (Context ctx2, T result, int index, object srcA, object srcB) => {
				var _onResult = srcA as Func<T, int, ValueTask>;
				await _onResult(result, index);
			}, onResult, null);

		}, writer, stream, includes, Filter == null ? IncludeTotal : Filter.IncludeTotal, currentSecondarySource != null);

		if (ReleaseFilter && Filter != null)
		{
			Filter.Release();
		}

		bool hasSecondary = false;
		IncludeSet includeSet = null;
		bool firstSecondary = true;

		if (currentSecondarySource != null)
		{
			hasSecondary = true;
			includeSet = ServiceForType.GetContentFields().GetIncludeSet(includes);
			writer.WriteASCII(",\"secondary\":{");
		}

		while (currentSecondarySource != null)
		{
			if (firstSecondary)
			{
				firstSecondary = false;
			}
			else
			{
				writer.Write((byte)',');
			}
			writer.WriteEscaped(currentSecondarySource.SourceName);
			writer.Write((byte)':');

			await currentSecondarySource.ToJson(context, writer, stream, includeSet);

			// Keep asking the original for its next source if there are more.
			currentSecondarySource = src.GetNextSource();
		}
		
		if (hasSecondary)
		{
			// Close both
			writer.WriteASCII("}}");
		}

		if (stream != null)
		{
			// Copy remaining bits:
			await writer.CopyToAsync(stream);
		}

		writer.Release();
	}

}

/// <summary>
/// Streams the given main type, plus then some additional results sets which can be non DB content types.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="ID"></typeparam>
public class MultiStreamSource<T, ID> : ContentStreamSource<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IEquatable<ID>, IComparable<ID>, IConvertible
{
	/// <summary>
	/// The primary source (typically either an AutoService or ListStreamSource).
	/// </summary>
	public ContentStreamSource<T, ID> PrimarySource;

	/// <summary>
	/// The first secondary source in a linked list.
	/// </summary>
	public SecondaryContentStreamSource FirstSecondarySource;

	/// <summary>
	/// The current secondary source (effectively the pointer in a linked list).
	/// </summary>
	public SecondaryContentStreamSource Current;


	/// <summary>
	/// Creates a new multi-stream src.
	/// </summary>
	/// <param name="primarySource"></param>
	/// <param name="secondaryChain"></param>
	public MultiStreamSource(ContentStreamSource<T, ID> primarySource, SecondaryContentStreamSource secondaryChain)
	{
		PrimarySource = primarySource;
		FirstSecondarySource = secondaryChain;
		Current = secondaryChain;
	}

	/// <summary>
	/// Resets the source such that iteration over the sets can happen again.
	/// </summary>
	public void Reset()
	{
		Current = FirstSecondarySource;
	}

	/// <summary>
	/// Starts streaming the results, invoking the given callback on each one.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filter"></param>
	/// <param name="onResult"></param>
	/// <param name="srcA"></param>
	/// <param name="srcB"></param>
	/// <returns></returns>
	public ValueTask<int> GetResults(Context context, Filter<T, ID> filter, Func<Context, T, int, object, object, ValueTask> onResult, object srcA, object srcB)
	{
		return PrimarySource.GetResults(context, filter, onResult, srcA, srcB);
	}

	/// <summary>
	/// Gets the next source if this stream has more than one.
	/// </summary>
	/// <returns></returns>
	public SecondaryContentStreamSource GetNextSource()
	{
		var active = Current;

		if (active != null)
		{
			Current = active.Next;
		}

		return active;
	}
}

/// <summary>
/// A convenience wrapper for streaming lists of content types.
/// Best avoided - use the mainline service output instead as it avoids allocating the list itself.
/// For quick wins or minorly used endpoints though, this is completely fine.
/// </summary>
public class ListStreamSource<T, ID> : ContentStreamSource<T, ID>
	where T: Content<ID>, new()
	where ID : struct, IEquatable<ID>, IComparable<ID>, IConvertible
{
	/// <summary>
	/// The list in this source.
	/// </summary>
	public IEnumerable<T> Content;

	/// <summary>
	/// Total number of results.
	/// </summary>
	public int? Total;

	/// <summary>
	/// Creates a new list source.
	/// </summary>
	/// <param name="content"></param>
	/// <param name="total">Optional total number of results.</param>
	public ListStreamSource(IEnumerable<T> content, int? total = null)
	{
		Content = content;
		Total = total;
	}

	/// <summary>
	/// Starts streaming the results, invoking the given callback on each one.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filter"></param>
	/// <param name="onResult"></param>
	/// <param name="srcA"></param>
	/// <param name="srcB"></param>
	/// <returns></returns>
	public async ValueTask<int> GetResults(Context context, Filter<T, ID> filter, Func<Context, T, int, object, object, ValueTask> onResult, object srcA, object srcB)
	{
		if(Content == null)
		{
			return 0;
		}

		var counter = 0;

		foreach (var item in Content)
		{
			await onResult(context, item, counter, srcA, srcB);
			counter++;
		}

		return Total.HasValue ? Total.Value : counter;
	}

	/// <summary>
	/// If this content stream has more than one source, this gets the next one.
	/// </summary>
	/// <returns></returns>
	public SecondaryContentStreamSource GetNextSource()
	{
		return null;
	}
}

/// <summary>
/// A convenience wrapper for streaming lists of content types.
/// Best avoided - use the mainline service output instead as it avoids allocating the list itself.
/// For quick wins or minorly used endpoints though, this is completely fine.
/// </summary>
public class SecondaryListStreamSource<T> : SecondaryContentStreamSource
{
	/// <summary>
	/// The list in this source.
	/// </summary>
	public IEnumerable<T> Content;

	/// <summary>
	/// Creates a new list source.
	/// </summary>
	/// <param name="srcName"></param>
	/// <param name="content"></param>
	public SecondaryListStreamSource(string srcName, IEnumerable<T> content)
	{
		SourceName = srcName;
		Content = content;
	}

	/// <summary>
	/// Gets the serializer for the type of content in this stream source.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public override async ValueTask<TypeReaderWriter> GetSerializer(Context context)
	{
		// Get a typeRW for T.
		return await TypeIOEngine.GetSerializer<T>(context);
	}

	/// <summary>
	/// Starts streaming the results, invoking the given callback on each one.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="onResult"></param>
	/// <param name="srcA"></param>
	/// <param name="srcB"></param>
	/// <returns></returns>
	public override async ValueTask<int> GetResults(Context context, Func<Context, object, int, object, object, ValueTask> onResult, object srcA, object srcB)
	{
		if(Content == null)
		{
			return 0;
		}

		var counter = 0;

		foreach (var item in Content)
		{
			await onResult(context, item, counter, srcA, srcB);
			counter++;
		}

		return counter;
	}
}
