using Api.AutoForms;
using Api.Database;
using Newtonsoft.Json;
using System;

namespace Api.Users
{
	/// <summary>
	/// Base class for content that can be auto-installed with tracking.
	/// </summary>
	public abstract class InstallableContent<ID> : VersionedContent<ID> where ID : struct
	{
		/// <summary>
		/// A unique key for this content, used for referencing and installation tracking.
		/// Typically generated.
		/// </summary>
		[Data("readonly", "true")]
		[DatabaseField(Length = 100)]
		public string Key;

		/// <summary>
		/// The latest RevisionId at the point of content install.
		/// </summary>
		[JsonIgnore]
		public uint? LastInstallRevisionId;

		/// <summary>
		/// The latest build time that an install check happened.
		/// </summary>
		[JsonIgnore]
		public DateTime? LastInstallBuildTimeUtc;
	}
}
