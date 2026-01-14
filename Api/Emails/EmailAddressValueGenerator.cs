using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;

namespace Api.Users;

#warning obsoleted by field rules
/// <summary>
/// A virtual field value generator for a field called "emailAddress" which returns the email address IF the user is "myself".
/// Only usable on User objects.
/// 
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class EmailAddressValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
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
	public override ValueTask GetValue(Context context, T forObject, Writer writer, ContextFlags flags)
    {
        User user = forObject as User;

        if (user == null)
        {
            // This include only works on user objects.
            writer.WriteASCII("null");
            return new ValueTask();
        }

        // Is it "myself"?
        if (context.UserId == user.Id)
        {
            // Yep!
            writer.WriteEscaped(user.Email);
        }
        else
        {
            // Nope go away!
            writer.WriteASCII("null");
		}

		return new ValueTask();
	}

    /// <summary>
    /// The type, if any, associated with the value being outputted. Must not vary.
    /// For example, if GetValue outputs only strings, this is typeof(string).
    /// </summary>
    /// <returns></returns>
    public override Type OutputType => typeof(string);

}