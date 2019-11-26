using System;
using System.Collections.Generic;


namespace Api.Eventing
{
	/// <summary>
	/// A set of actual event methods to run.
	/// </summary>
	/// <typeparam name="T">A Func type representing the method signature.</typeparam>
	public class EventMethodSet<T> where T : class {

		/// <summary>
		/// The raw array of methods.
		/// </summary>
		public T[] Methods = new T[5];

		/// <summary>
		/// The priorities of each of the methods.
		/// This is always the same size as the methods array.
		/// It's stored separately to minimise the dispatch hot path.
		/// </summary>
		public int[] Priorities = new int[5];

		/// <summary>
		/// The actual number of methods currently listening here.
		/// </summary>
		public int HandlerCount = 0;

		/// <summary>
		/// Removes the given method from this set.
		/// Returns true if it actually did something, or false if not.
		/// </summary>
		/// <param name="method"></param>
		public bool Remove(T method)
		{
			lock (this)
			{
				// Find the index:
				var index = -1;
				for (var i = 0; i < Methods.Length; i++)
				{
					if (Methods[i] == method)
					{
						index = i;
						break;
					}
				}

				if (index == -1)
				{
					return false;
				}

				// Note we won't downsize the array. Just remove the method by moving stuff after it over.
				HandlerCount--;
				
				for (var i = index; i < HandlerCount; i++)
				{
					Methods[i] = Methods[i + 1];
				}

			}

			return true;
		}

		/// <summary>
		/// Adds a handler of the given priority, returning the array index to add it at.
		/// </summary>
		/// <param name="method">The method that will run at this priority.</param>
		/// <param name="priority">The priority value to add.</param>
		/// <returns></returns>
		public void Add(T method, int priority)
		{
			lock (this)
			{
				// Notes here:
				// 1. It's an array to remove all list overhead from the runtime performance.
				// 2. It'll almost always be really tiny, 
				//    so the fastest way to check priority is to loop over it linearly.

				// So first find the index:
				var indexToAddAt = HandlerCount;

				for (var i = 0; i < HandlerCount; i++)
				{
					if (Priorities[i] > priority)
					{
						// Stop there. it goes at index i.
						indexToAddAt = i;
						break;
					}
				}

				// Do we need to resize the array?
				if (HandlerCount == Priorities.Length)
				{
					// Add another 5 to the size of the array:
					var newPriorities = new int[Priorities.Length + 5];
					var newMethods = new T[newPriorities.Length];

					// Copy existing priorities/ methods:
					Array.Copy(Priorities, newPriorities, HandlerCount);
					Array.Copy(Methods, newMethods, HandlerCount);

					Priorities = newPriorities;
					Methods = newMethods;
				}

				// Bump handler count up:
				HandlerCount++;

				// Next move over any values that are at or after the index we're adding at.
				for (var i = HandlerCount - 1; i > indexToAddAt; i--)
				{
					Priorities[i] = Priorities[i - 1];
					Methods[i] = Methods[i - 1];
				}

				// Put the values there:
				Priorities[indexToAddAt] = priority;
				Methods[indexToAddAt] = method;
			}
		}


	}
	
}
