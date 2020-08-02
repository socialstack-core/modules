
namespace Api.Database
{

	/// <summary>
	/// A thing with an ID.
	/// </summary>
	public interface IHaveId
	{
		/// <summary>
		/// Gets the ID for this thing.
		/// </summary>
		/// <returns></returns>
		int GetId();
	}
}