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
		/// Puts a constant value into the target field.
		/// </summary>
		public void AddConstant(Type toType, string toFieldname, object constValue)
		{
			var toField = new Field()
			{
				OwningType = toType,
				Name = toFieldname
			};

			Transfers.Add(new FieldTransfer()
			{
				IsConstant = true,
				Constant = constValue,
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
		/// True if it's just a constant value to use for the target field.
		/// </summary>
		public bool IsConstant;
		
		/// <summary>
		/// The constant to use. Can also be a null.
		/// </summary>
		public object Constant;
		
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