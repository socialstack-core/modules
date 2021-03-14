
namespace Api.Database
{

	/// <summary>
	/// A thing with an ID.
	/// </summary>
	public interface IHaveId<T> where T:struct
	{
		/// <summary>
		/// Gets the ID for this thing.
		/// </summary>
		/// <returns></returns>
		T GetId();

		/// <summary>
		/// Sets the ID for this thing.
		/// </summary>
		/// <param name="id"></param>
		void SetId(T id);
	}
}