namespace Api.SocketServerLibrary{
	
	/// <summary>
	/// This tracks message objects which are waiting for responses.
	/// Note that this can return a -1 request ID. When that happens, there are essentially ~1000 "in flight" requests already on a particular connection
	/// so instead the message must wait in the send queue.
	/// </summary>
	public class RequestIdStack{
		
		/// <summary>
		/// Max stack size.
		/// </summary>
		public const int MaxSize = 1000;
		/// <summary>
		/// True if the stack was out of IDs because it reached the max.
		/// </summary>
		private bool WasSaturated;
		/// <summary>
		/// Current available IDs.
		/// </summary>
		private int AvailableIdCount;
		/// <summary>
		/// Indicies that are currently not in use. This functions as a stack.
		/// </summary>
		private int[] AvailableIds;
		/// <summary>
		/// Index in this array is the request ID.
		/// </summary>
		private IMessage[] Messages;
		
		
		/// <summary>
		/// Create a new request ID stack.
		/// </summary>
		public RequestIdStack(){
			Setup(10);
		}
		
		/// <summary>
		/// Sets up the stack with the given amount of space.
		/// </summary>
		/// <param name="size"></param>
		private void Setup(int size){
			AvailableIds = new int[size];
			Messages = new IMessage[size];
			
			for(var i=0;i<size;i++){
				AvailableIds[i] = i;
			}
			
			// Stack pointer in AvailableIds.
			AvailableIdCount = size;
		}

		/// <summary>
		/// Resizes the internal buffer.
		/// </summary>
		private void Expand(){
			lock(this){
				// Double the current size, capped to MaxSize.
				var targetSize = AvailableIds.Length * 2;
				
				if(targetSize > MaxSize){
					targetSize = MaxSize;
				}
				
				var newIds = new int[targetSize];
				var newMessages = new IMessage[targetSize];
				
				// Transfer message refs:
				System.Array.Copy(Messages, 0, newMessages, 0, Messages.Length);
				
				// We know for sure AvailableIds is effectively just junk right now, so all we need to do is stack the new IDs.
				// Stack up all our new IDs:
				for(var i=AvailableIds.Length;i<targetSize;i++){
					newIds[AvailableIdCount++] = i;
				}
				
				Messages = newMessages;
				AvailableIds = newIds;
			}
		}
		
		/// <summary>
		/// Get a message when a request with the given ID has returned.
		/// </summary>
		public IMessage RequestHasReturned(int requestId){
			var ctx = Messages[requestId];
			
			// Return the ID to the pool:
			AvailableIds[AvailableIdCount] = requestId;
			AvailableIdCount++;

			if (WasSaturated)
			{
				WasSaturated = false;
				OnIdAvailable();
			}

			return ctx;
		}

		/// <summary>
		/// Called when this pool was saturated but now has 1 or more ID's available.
		/// </summary>
		protected virtual void OnIdAvailable() {

		}

		/// <summary>Note that the callback for these is either opcode 40 or 41. This returns -1 if the stack is currently exhausted.</summary>
		public int GetRequestId(IMessage msg){
			
			if(AvailableIdCount == 0){
				// No available IDs. Can we expand?
				if(Messages.Length == MaxSize){
					// Nope!
					WasSaturated = true;
					return -1;
				}
				
				// Expand the array now:
				Expand();
			}
			
			// Pop a request ID:
			AvailableIdCount--;
			var newId = AvailableIds[AvailableIdCount];
			
			// Apply the message now:
			Messages[newId] = msg;
			return newId;
		}
	}
}