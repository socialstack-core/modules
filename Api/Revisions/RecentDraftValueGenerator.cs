using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;
using Api.Users;
namespace Api.Revisions;


/// <summary>
/// A virtual field value generator for a field called "recentDraft".
/// You can include this field on any type and it will provide the ID of a draft that is more recent than the current type, or zero.
/// 
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class RecentDraftValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	/// <summary>
	/// Generate the value.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="forObject"></param>
	/// <param name="writer"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public override async ValueTask GetValue(Context context, T forObject, Writer writer, ContextFlags flags)
	{
		var revisions = Service.Revisions;
		var timestamps = forObject as IHaveTimestamps;

		if (revisions != null && timestamps != null)
		{
			var newerDraft = await revisions.GetNewerDraft(context, timestamps.GetEditedUtc(), forObject.Id);

			if (newerDraft != null)
			{
				writer.WriteS(Service.ReverseId(newerDraft.Id));
				return;
			}
		}

		writer.WriteASCII("0");
		return;
	}

	/// <summary>
	/// The type, if any, associated with the value being outputted.
	/// For example, if GetValue outputs only strings, this is typeof(string).
	/// </summary>
	/// <returns></returns>
	public override Type OutputType => typeof(ulong);
}