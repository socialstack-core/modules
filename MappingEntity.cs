using Api.SocketServerLibrary;
using System;


namespace Api.Database
{
	
	/// <summary>
	/// Maps e.g. tags to particular content.
	/// </summary>
	public abstract class MappingEntity : Content<uint>
	{
		/// <summary>
		/// The type ID of the tagged content. See also: Api.Database.ContentTypes
		/// </summary>
		public int ContentTypeId;
		/// <summary>
		/// The ID of the tagged content.
		/// </summary>
		public uint ContentId;
		/// <summary>
		/// The UTC creation date. Read/ delete only rows so an edited date isn't present here.
		/// </summary>
		public DateTime CreatedUtc;
		/// <summary>
		/// The ID of a particular revision that these tags are on. Zero if it's on the live content.
		/// </summary>
		public uint RevisionId;

		/// <summary>
		/// Target content ID (e.g. ID of the target tag).
		/// </summary>
		public virtual uint TargetContentId
		{
			get
			{
				return 0;
			}
			set
			{			
			}
		}
	}

	/// <summary>
	/// Maps e.g. tags to particular content type. Often Mapping[uint, uint].
	/// If it's e.g. a list of tags on a user, it has a UserId field and a TagId field, which are linked up to SourceId and TargetId.
	/// These are fully automated and used via the include system.
	/// </summary>
	public class Mapping<S,T> : Content<uint> 
		where S : struct
		where T : struct
	{
		/// <summary>
		/// The UTC creation date. Read/ delete only rows so an edited date isn't present here.
		/// </summary>
		public DateTime CreatedUtc;

		/// <summary>
		/// Source ID.
		/// </summary>
		public virtual S SourceId
		{
			get {
				return default(S);
			}
			set {
				
			}
		}

		/// <summary>
		/// Target ID.
		/// </summary>
		public virtual T TargetId
		{
			get
			{
				return default(T);
			}
			set
			{

			}
		}

		/// <summary>
		/// Writes ID,SourceId,TargetId to the given writer, in textual format.
		/// </summary>
		/// <param name="w"></param>
		public virtual void ToJson(Writer w)
		{
			throw new Exception("Not implemented");
		}
	}
}