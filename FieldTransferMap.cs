using Api.Database;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.Database
{

	/// <summary>
	/// A mapping of from -> to fields.
	/// </summary>
	public class FieldTransferMap
	{
		/// <summary>
		/// The set of transfers.
		/// </summary>
		public List<FieldTransfer> Transfers = new List<FieldTransfer>();


		/// <summary>
		/// Adds a transfer to this map.
		/// </summary>
		/// <param name="fromType"></param>
		/// <param name="fromFieldName"></param>
		/// <param name="toType"></param>
		/// <param name="toFieldName"></param>
		public void Add(Type fromType, string fromFieldName, Type toType, string toFieldName)
		{

			var fromField = new Field()
			{
				OwningType = fromType,
				Name = fromFieldName
			};

			var toField = new Field()
			{
				OwningType = toType,
				Name = toFieldName
			};

			fromField.SetFullName(SourceTypeNameExtension);
			toField.SetFullName(TargetTypeNameExtension);

			Transfers.Add(new FieldTransfer()
			{
				From = fromField,
				To = toField
			});

		}

		/// <summary>
		/// An extension to add to the source table name.
		/// </summary>
		public string SourceTypeNameExtension;

		/// <summary>
		/// An extension to add to the target table name.
		/// </summary>
		public string TargetTypeNameExtension;

		/// <summary>
		/// The type of the source row.
		/// </summary>
		public Type SourceType
		{
			get
			{
				return Transfers[0].From.OwningType;
			}
		}

		/// <summary>
		/// The type of the target row.
		/// </summary>
		public Type TargetType
		{
			get
			{
				return Transfers[0].To.OwningType;
			}
		}
	}

	/// <summary>
	/// A single from->to field mapping.
	/// </summary>
	public class FieldTransfer
	{

		/// <summary>
		/// The source field.
		/// </summary>
		public Field From;

		/// <summary>
		/// The target field.
		/// </summary>
		public Field To;

	}
}