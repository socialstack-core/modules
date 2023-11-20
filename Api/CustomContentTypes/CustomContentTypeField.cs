using System;
using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.CustomContentTypes
{

	/// <summary>
	/// A custom content type field.
	/// </summary>
	[ListAs("CustomContentTypeFields")]
	[ImplicitFor("CustomContentTypeFields", typeof(CustomContentType))]
	[HasVirtualField("CustomContentType", typeof(CustomContentType), "CustomContentTypeId")]
	public partial class CustomContentTypeField : VersionedContent<uint>
	{
		/// <summary>
		/// The content type that this field belongs to.
		/// </summary>
		public uint CustomContentTypeId;
		
		/// <summary>
		/// This fields default value.
		/// </summary>
		public string DefaultValue;
		
		/// <summary>
		/// The type of this field.
		/// </summary>
		public string DataType;

		/// <summary>
		/// The entity that this field links to (if DataType is 'entity')
		/// </summary>
		public string LinkedEntity;

		/// <summary>
		/// The name of the field, used by socialstack.
		/// </summary>
		[Module(Hide = true)]
		public string Name;

		/// <summary>
		/// The human readable nickname of this field.
		/// </summary>
		public string NickName;

		/// <summary>
		/// True if this field localised
		/// </summary>
		public bool Localised;

		/// <summary>
		/// True if this field should be url encoded
		/// </summary>
		public bool UrlEncoded;		
		
		/// <summary>
		/// True if this field should be hidden
		/// </summary>
		public bool IsHidden;

		/// <summary>
        /// True if this date time field should hide seconds
        /// </summary>
        public bool HideSeconds;

        /// <summary>
        /// True if this date time field should be rounded to the nearest 5 minutes
        /// </summary>
        public bool RoundMinutes;

        /// <summary>
		/// The validation to be applied to this field
		/// </summary>
		public string Validation;

		/// <summary>
		/// The order of this field on the form
		/// </summary>
		public uint Order;

        /// <summary>
        /// The group/section to link together associatd elements on the form
        /// </summary>
        public string Group;

        /// <summary>
        /// Should the dropdown options follow currency locale?
        /// </summary>
        public bool OptionsArePrices;

		/// <summary>
		/// Has this field been deleted?
		/// </summary>
		[Module(Hide = true)]
		public bool Deleted;

		private string _metaType;

		/// <summary>
		/// Set the meta type of the field. This is used to determine how to render the field on the form.
		/// </summary>
		/// <param name="metaType"></param>
		public void SetMetaType(string metaType)
        {
			_metaType = metaType;
		}

		/// <summary>
		/// Get the meta type of the field. This is used to determine how to render the field on the form.
		/// </summary>
		/// <returns></returns>
		public String GetMetaType()
        {
			return _metaType;
        }
	}

}