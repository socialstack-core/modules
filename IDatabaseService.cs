using Api.Permissions;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Database
{
	/// <summary>
	/// Handles communication with the sites database.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	
	// Used by almost all services so has a very high load order priority.
	[LoadPriority(1)]
	public partial interface IDatabaseService
    {
		/// <summary>
		/// Runs the given query using the given arguments to bind.
		/// Does not return any values other than a true/ false if it succeeded.
		/// </summary>
		/// <param name="q"></param>
		/// <param name="srcObject"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<bool> Run<T>(Query<T> q, T srcObject, params object[] args) where T : DatabaseRow;

		/// <summary>
		/// Performs a bulk insert.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="q"></param>
		/// <param name="toInsertSet"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<bool> Run<T>(Query<T> q, List<T> toInsertSet, params object[] args);

		/// <summary>
		/// Usually used for bulk deletes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="q"></param>
		/// <param name="idsToDelete"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<bool> Run<T>(Query<T> q, IEnumerable<int> idsToDelete, params object[] args);

		/// <summary>
		/// Generic run. Usually used to delete by Id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="q"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<bool> Run<T>(Query<T> q, params object[] args);

		/// <summary>
		/// Generic run. Usually used to delete by Id.
		/// </summary>
		/// <param name="q"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<bool> Run(Query q, params object[] args);

		/// <summary>
		/// Run a raw query with no arguments. Avoid when possible.
		/// </summary>
		/// <param name="query">The query to run.</param>
		/// <returns></returns>
		Task<bool> Run(string query);

		/// <summary>
		/// Runs the given query with the given args to bind. Returns the results mapped as the given object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="q"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<T> Select<T>(Query<T> q, params object[] args) where T : new();

		/// <summary>
		/// Runs the given query with the given args to bind. Returns the results mapped as the given object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="q"></param>
		/// <param name="filter">
		/// A runtime filter to apply to the query. It's the same as WHERE.
		/// These filters often handle permission based filtering. 
		/// Pass it to Capability.IsGranted to have that happen automatically.</param>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<List<T>> List<T>(Query<T> q, Filter filter, params object[] args) where T : new();
		
		/// <summary>
		/// Database text escape. You should instead be using the args set (and ? placeholders).
		/// </summary>
		/// <param name="value">The text to escape.</param>
		/// <returns></returns>
		string Escape(string value);
		
    }
}
