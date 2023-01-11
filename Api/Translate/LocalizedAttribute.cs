using System;

namespace Api.Translate
{
	/// <summary>
	/// Use this to declare a database field as localized - either translatable or containing different values per locale.
	/// Fields marked with this will result in another table being generated.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class LocalizedAttribute : Attribute
	{
		public LocalizedAttribute() { }
	}
}
