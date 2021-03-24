using Api.Eventing;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.Startup
{

	/// <summary>
	/// A utility system for syncing content across a cluster.
	/// </summary>
	public static class RemoteSync
	{

		/// <summary>
		/// Adds the given type to the remote synced type set.
		/// Fires an event when the set changes.
		/// </summary>
		public static void Add(Type type)
		{
			foreach(var t in All)
			{
				if(t == type)
				{
					return;
				}
			}
			
			// Add it:
			All.Add(type);
			
			// Trigger evt:
			Events.RemoteSyncTypeAdded.Dispatch(new Contexts.Context(), type, All.Count - 1).Wait();
		}

		/// <summary>
		/// Underlying synced types.
		/// </summary>
		public static List<Type> All { get; } = new List<Type>();

	}
}