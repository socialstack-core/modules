using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Api.ContentSync
{
	/// <summary>
	/// Sync table stored in a file. Used by devs.
	/// </summary>
	public class SyncTableFile<T> : SyncTableFile where T:DatabaseRow, new()
	{
		private string FilePath;
		
		/// <summary>
		/// Sync table file obj for the given complete filepath. Usually of the form Database/username_data.txt
		/// </summary>
		/// <param name="filePath"></param>
		public SyncTableFile(string filePath){
			FilePath = filePath;
		}
		
		/// <summary>
		/// The service which deals with this type (if there is one).
		/// </summary>
		public AutoService<T> Service;

		/// <summary>
		/// Used to build strings in the file.
		/// </summary>
		private StringBuilder builder = new StringBuilder();

		private StreamWriter Writer;

		/// <summary>
		/// Reads until the given delimiter.
		/// </summary>
		/// <param name="sr"></param>
		/// <param name="delimiter"></param>
		/// <returns></returns>
		private string ReadUntilDelimiter(StreamReader sr, char delimiter)
		{
			var next = sr.Read();
			if (next == -1)
			{
				return null;
			}

			builder.Clear();

			while (next != -1 && next != delimiter)
			{
				builder.Append((char)next);
				next = sr.Read();
			}

			return builder.ToString();
		}

		private Dictionary<string, FieldInfo> FieldMap;
		private FieldInfo[] FieldSet;

		private void LoadFieldMap()
		{
			// It's always fields for the DB.
			// Don't need to deal with properties.
			// See also: DatabaseDiffService.cs
			FieldMap = new Dictionary<string, FieldInfo>();

			var fields = typeof(T).GetFields();
			FieldSet = fields;

			for (var i = 0; i < fields.Length; i++)
			{
				var field = fields[i];

				FieldMap[field.Name] = field;
			}
		}

		/// <summary>
		/// Sets the service ref (can be null).
		/// </summary>
		/// <param name="service"></param>
		public override void SetService(object service)
		{
			Service = (AutoService<T>)service;
		}

		/// <summary>
		/// Applies a field by its name and with the given value to the given row object.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="fieldName"></param>
		/// <param name="fieldValue"></param>
		private void ApplyField(T row, string fieldName, string fieldValue)
		{
			if (FieldMap == null)
			{
				LoadFieldMap();
			}

			if (!FieldMap.TryGetValue(fieldName, out FieldInfo field))
			{
				// This field has since been removed.
				return;
			}

			// Based on the field type, attempt to map the value to it.
			var fieldValueType = field.FieldType;

			// Special case for byte[]:
			if (fieldValueType == typeof(byte[]))
			{
				// It was hex encoded (hex being selected as it's the format used in SQL):
				field.SetValue(row, Hex.Convert(fieldValue));
				return;
			}

			// Other array types - we don't handle:
			if (fieldValueType.IsArray)
			{
				return;
			}

			var nullableBaseType = Nullable.GetUnderlyingType(field.FieldType);
			var nullable = false;

			if (nullableBaseType != null)
			{
				nullable = true;

				// This is a nullable field.
				fieldValueType = nullableBaseType;
			}

			switch (Type.GetTypeCode(fieldValueType))
			{
				case TypeCode.Boolean:
					field.SetValue(row, fieldValue == "1");
					break;
				case TypeCode.Char:
					if (fieldValue == null || fieldValue.Length == 0)
					{
						field.SetValue(row, nullable ? null : (object)'\0');
					}
					else
					{
						field.SetValue(row, fieldValue[0]);
					}
					break;
				case TypeCode.SByte:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (sbyte.TryParse(fieldValue, out sbyte sb))
					{
						field.SetValue(row, sb);
					}
					break;
				case TypeCode.Byte:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (byte.TryParse(fieldValue, out byte b))
					{
						field.SetValue(row, b);
					}
					break;
				case TypeCode.Int16:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (short.TryParse(fieldValue, out short s))
					{
						field.SetValue(row, s);
					}
					break;
				case TypeCode.UInt16:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (ushort.TryParse(fieldValue, out ushort us))
					{
						field.SetValue(row, us);
					}
					break;
				case TypeCode.Int32:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (int.TryParse(fieldValue, out int i))
					{
						field.SetValue(row, i);
					}
					break;
				case TypeCode.UInt32:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (uint.TryParse(fieldValue, out uint ui))
					{
						field.SetValue(row, ui);
					}
					break;
				case TypeCode.Int64:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (long.TryParse(fieldValue, out long l))
					{
						field.SetValue(row, l);
					}
					break;
				case TypeCode.UInt64:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (ulong.TryParse(fieldValue, out ulong ul))
					{
						field.SetValue(row, ul);
					}
					break;
				case TypeCode.Single:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (float.TryParse(fieldValue, out float flt))
					{
						field.SetValue(row, flt);
					}
					break;
				case TypeCode.Double:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (double.TryParse(fieldValue, out double doub))
					{
						field.SetValue(row, doub);
					}
					break;
				case TypeCode.Decimal:
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (decimal.TryParse(fieldValue, out decimal d))
					{
						field.SetValue(row, d);
					}
					break;
				case TypeCode.DateTime:
					// Serialized as Ticks, UTC
					if (nullable && string.IsNullOrEmpty(fieldValue))
					{
						field.SetValue(row, null);
					}
					else if (long.TryParse(fieldValue, out long ticks))
					{
						field.SetValue(row, new DateTime(ticks, DateTimeKind.Utc));
					}
					break;
				case TypeCode.String:
					field.SetValue(row, fieldValue);
					break;
				default:
					return;
			}

		}

		/// <summary>
		/// Applies any newly detected changes in this set now.
		/// </summary>
		/// <param name="offset"></param>
		/// <returns></returns>
		public override async Task<long> Sync(long offset)
		{
			if (Service == null)
			{
				// Unchanged
				return offset;
			}

			var ctx = new Context();

			var changes = 0;

			var totalLength = await ReadRows(offset, async (char mode, T row, int localeIdOrDeletedId) => {
				changes++;

				switch (mode)
				{
					case 'C':
						// Create the entry:
						ctx.LocaleId = localeIdOrDeletedId;

						// Already exists?
						var existing = await Service.Get(ctx, row.Id);

						if (existing != null)
						{
							// Update instead:
							await Service.Update(ctx, row);
						}
						else
						{
							await Service.Create(ctx, row);
						}
					break;
					case 'D':
						// Delete the entry:
						var existingDel = await Service.Get(ctx, localeIdOrDeletedId);

						if (existingDel != null)
						{
							ctx.LocaleId = 0;
							await Service.Delete(ctx, localeIdOrDeletedId);
						}
					break;
					case 'U':
						// Update the entry:
						ctx.LocaleId = localeIdOrDeletedId;
						var existingUpd = await Service.Get(ctx, row.Id);

						if (existingUpd != null)
						{
							await Service.Update(ctx, row);
						}
						// Otherwise do nothing.
					break;
				}
				
			});

			if (changes != 0)
			{
				Console.WriteLine("ContentSync applied " + changes + " change(s) for " + typeof(T).Name);
			}

			return totalLength;
		}
		
		/// <summary>
		/// Reads until \r\n or just \n.
		/// </summary>
		/// <param name="sr"></param>
		/// <returns></returns>
		private string ReadUntilEol(StreamReader sr)
		{
			var next = sr.Read();
			if (next == -1)
			{
				return null;
			}

			builder.Clear();

			while (next != -1)
			{
				if (next == '\r')
				{
					sr.Read();
					break;
				}
				else if (next == '\n')
				{
					break;
				}

				builder.Append((char)next);
				next = sr.Read();
			}

			return builder.ToString();
		}

		/// <summary>
		/// Write a list of rows now.
		/// </summary>
		/// <param name="rows"></param>
		/// <param name="mode">C for create, D for deleted, U for updated.</param>
		/// <param name="localeId"></param>
		public void Write(IEnumerable<T> rows, char mode, int localeId)
		{
			foreach (var row in rows)
			{
				WriteNoFlush(row, mode, localeId);
			}

			if (Writer != null)
			{
				Writer.Flush();
				Writer.Close();
				Writer = null;
			}
		}

		/// <summary>
		/// Writes the given row object to the file now.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="mode">C for create, D for deleted, U for updated.</param>
		/// <param name="localeId"></param>
		public override void Write(object row, char mode, int localeId)
		{
			WriteNoFlush((T)row, mode, localeId);

			if (Writer != null)
			{
				Writer.Flush();
				Writer.Close();
				Writer = null;
			}
		}

		/// <summary>
		/// Writes the given row object to the file now.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="mode">C for create, D for deleted, U for updated.</param>
		/// <param name="localeId"></param>
		public void WriteNoFlush(T row, char mode, int localeId)
		{
			if (FieldMap == null)
			{
				LoadFieldMap();
			}

			if (Writer == null)
			{
				Writer = new StreamWriter(FilePath, true);
			}

			Writer.Write(mode);

			if (mode == 'D')
			{
				// ID field only:
				Writer.Write(row.Id.ToString());
				Writer.Write('\n');
				return;
			}

			// Write the field count:
			Writer.Write(FieldSet.Length + "," + localeId);
			Writer.Write('\n');

			for (var i = 0; i < FieldSet.Length; i++)
			{
				var field = FieldSet[i];
				var fieldName = field.Name;
				var fieldValueRaw = field.GetValue(row);

				if (fieldValueRaw == null)
				{
					Writer.Write(fieldName + "=-1\n");
					continue;
				}

				string fieldValue;
				
				var fieldValueType = field.FieldType;

				if (fieldValueType == typeof(byte[]))
				{
					// Hex encode it:
					fieldValue = Hex.Convert((byte[])fieldValueRaw);
				}
				else if (fieldValueType == typeof(DateTime))
				{
					// It's in ticks:
					fieldValue = ((DateTime)fieldValueRaw).Ticks.ToString();
				}
				else
				{
					// Other array types - we don't handle:
					if (fieldValueType.IsArray)
					{
						continue;
					}

					// Check if it's nullable:
					var nullableBaseType = Nullable.GetUnderlyingType(fieldValueType);

					if (nullableBaseType != null)
					{
						// This is a nullable field.
						fieldValueType = nullableBaseType;
					}

					if (Type.GetTypeCode(fieldValueType) == TypeCode.Object)
					{
						// Ignore generic objects.
						continue;
					}

					// Otherwise we just tostring it:
					fieldValue = fieldValueRaw.ToString();
				}

				// Write field=Length
				Writer.Write(fieldName + "=" + fieldValue.Length.ToString() + "\n");
				Writer.Write(fieldValue);

				// And a newline after the value for tidiness:
				Writer.Write('\n');
			}

			Writer.Write('\n');
		}

		/// <summary>
		/// Read the rows from the file now.
		/// </summary>
		public async Task<long> ReadRows(long offset, Func<char, T, int, Task> onReadRow)
		{
			long size = 0;

			try
			{

				using (StreamReader sr = new StreamReader(FilePath))
				{
					sr.BaseStream.Seek(offset, SeekOrigin.Begin);
					size = sr.BaseStream.Length;

					// Format is:
					/*
					FIELDCOUNT
					FIELDNAME=LENGTH
					FIELDVALUE_LENGTH_CHARACTERS
					FIELDNAME=LENGTH
					..
					*/
					while (true)
					{
						int mode = sr.Read();

						if (mode == -1)
						{
							// Indicates EOF.
							break;
						}

						if (mode == 'D')
						{
							// Deleted something.
							string idTxt = ReadUntilEol(sr);

							if (!int.TryParse(idTxt, out int deletedId))
							{
								break;
							}

							// 
							await onReadRow((char)mode, null, deletedId);

							continue;
						}

						string fieldCountTxt = ReadUntilDelimiter(sr, ',');
						string localeIdTxt = ReadUntilEol(sr);

						if (string.IsNullOrEmpty(fieldCountTxt))
						{
							// Indicates EOF.
							break;
						}

						if (!int.TryParse(fieldCountTxt, out int fieldCount))
						{
							break;
						}

						if (!int.TryParse(localeIdTxt, out int localeId))
						{
							break;
						}

						var row = new T();

						// Read that many fields:
						for (var i = 0; i < fieldCount; i++)
						{
							string fieldName = ReadUntilDelimiter(sr, '=');
							string fieldSizeTxt = ReadUntilEol(sr);

							if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(fieldSizeTxt))
							{
								break;
							}

							if (!int.TryParse(fieldSizeTxt, out int fieldSize))
							{
								break;
							}

							// Read a string of length fieldSize (it's always a string):
							builder.Clear();

							string fieldValue;

							if (fieldSize == -1)
							{
								fieldValue = null;
							}
							else
							{
								for (var c = 0; c < fieldSize; c++)
								{
									var character = sr.Read();
									if (character == -1)
									{
										break;
									}
									builder.Append((char)character);
								}

								// Value is followed by a newline:
								var newlineTest = sr.Read();

								if (newlineTest == -1)
								{
									break;
								}

								if (newlineTest == '\r')
								{
									newlineTest = sr.Read();
								}

								if (newlineTest != '\n')
								{
									break;
								}

								fieldValue = builder.ToString();
							}


							ApplyField(row, fieldName, fieldValue);
						}

						// EOL - may be \r\n or just \n - is required to indicate successful row completion:
						var eol = sr.Read();

						if (eol == '\r')
						{
							eol = sr.Read();
						}

						if (eol != '\n')
						{
							break;
						}

						await onReadRow((char)mode, row, localeId);
					}
				}
			}
			catch (DirectoryNotFoundException)
			{
				return 0;
			}
			catch (FileNotFoundException)
			{
				return 0;
			}

			return size;
		}
		
	}

	/// <summary>
	/// Base class of a sync table file.
	/// </summary>
	public class SyncTableFile
	{
		/// <summary>
		/// Sets the service ref (can be null).
		/// </summary>
		/// <param name="service"></param>
		public virtual void SetService(object service)
		{
		}

		/// <summary>
		/// Applies any newly detected changes in this set now.
		/// </summary>
		/// <param name="offset"></param>
		/// <returns></returns>
		public virtual Task<long> Sync(long offset)
		{
			return null;
		}

		/// <summary>
		/// Writes the given row object to the file now.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="mode">C for create, D for deleted, U for updated.</param>
		/// <param name="localeId"></param>
		public virtual void Write(object row, char mode, int localeId)
		{
		}

	}

	/// <summary>
	/// A group of sync table files, stored in a directory.
	/// </summary>
	public class SyncTableFileSet
	{
		/// <summary>
		/// The parent directory.
		/// </summary>
		public string ParentDirectory;

		/// <summary>
		/// Creates a sync table set.
		/// </summary>
		/// <param name="dir"></param>
		public SyncTableFileSet(string dir)
		{
			ParentDirectory = dir;
		}

		/// <summary>
		/// The file set.
		/// </summary>
		public Dictionary<string, SyncTableFile> Files = new Dictionary<string, SyncTableFile>();

		/// <summary>
		/// Applies any newly detected changes in this set now.
		/// </summary>
		/// <param name="databaseService"></param>
		/// <returns></returns>
		public async Task Sync(DatabaseService databaseService)
		{
			// First, load the index. This file indicates where we last got up to (the file length of each file in this set).
			var index = new SyncTableIndex(ParentDirectory + "/index.json");
			index.Setup();

			var indexChanged = false;

			foreach (var kvp in Files)
			{
				// Ask it to sync now:
				var srcIndex = index.Get(kvp.Key);
				long newIndex = await kvp.Value.Sync(srcIndex);

				if (srcIndex != newIndex)
				{
					indexChanged = true;
					index.Set(kvp.Key, newIndex);
				}
			}

			if (indexChanged)
			{
				index.Save();
			}
		}

		/// <summary>
		/// Sets up this sync table set.
		/// </summary>
		/// <param name="ifExistsOnly">Will only add entries to the table if the file exists.</param>
		public void Setup(bool ifExistsOnly)
		{
			// var schema = databaseService.Schema;

			// Future todo: Instead of content types, go over the actual tables in the schema.
			// This will allow it to cover off e.g. revisions.

			foreach (var kvp in ContentTypes.TypeMap)
			{
				// Get the service for this thing (if there is one):
				var svc = Services.Get("I" + kvp.Value.Name + "Service");

				var tableName = kvp.Value.TableName();
				var filePath = ParentDirectory + "/" + tableName + ".txt";
				if (ifExistsOnly && !File.Exists(filePath))
				{
					continue;
				}

				// Get the table file type now:
				var stfType = typeof(SyncTableFile<>).MakeGenericType(kvp.Value);

				// Instance it:
				var stf = (SyncTableFile)Activator.CreateInstance(stfType, new object[] { filePath });

				// Set service:
				stf.SetService(svc);

				// Add to lookup:
				Files[tableName] = stf;
			}

		}
	}

	/// <summary>
	/// A map of table name to the file length processed so far. Stored in a json file which is *not* retained in git, as it's unique to "me".
	/// </summary>
	public class SyncTableIndex
	{
		private string FilePath;
		/// <summary>
		/// A map of table name to the file length processed so far.
		/// </summary>
		private Dictionary<string, long> TableNameToLength;

		/// <summary>
		/// Creates the sync table index.
		/// </summary>
		/// <param name="file"></param>
		public SyncTableIndex(string file)
		{
			FilePath = file;
		}

		/// <summary>
		/// Writes the index out to a file.
		/// </summary>
		public void Save()
		{
			var json = JsonConvert.SerializeObject(TableNameToLength);
			File.WriteAllText(FilePath, json);
		}

		/// <summary>
		/// Loads the set from the json file.
		/// </summary>
		public void Setup()
		{
			TableNameToLength = new Dictionary<string, long>();

			try
			{
				var json = File.ReadAllText(FilePath);
				TableNameToLength = JsonConvert.DeserializeObject<Dictionary<string, long>>(json);
			}
			catch (DirectoryNotFoundException)
			{
				// Doesn't exist - all 0's anyway.
			}
			catch (FileNotFoundException)
			{
				// Doesn't exist - all 0's anyway.
			}
		}

		/// <summary>
		/// Sets the given key, or creates it.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="size"></param>
		public void Set(string key, long size)
		{
			TableNameToLength[key] = size;
		}

		/// <summary>
		/// Gets the given key, if it exists.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public long Get(string key)
		{
			TableNameToLength.TryGetValue(key, out long result);
			return result;
		}

	}
}