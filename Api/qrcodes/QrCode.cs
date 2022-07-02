using System;
using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.QrCodes
{
	
	/// <summary>
	/// A QrCode
	/// </summary>
	public partial class QrCode : VersionedContent<uint>
	{
        /// <summary>
        /// The name of the qrCode
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Name;
		
        /// <summary>
        /// The URL content in the QR code.
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		[Module("Admin/QrPreview")]
		public string Url;
		
	}

}