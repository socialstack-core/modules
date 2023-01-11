using Api.AutoForms;
using Api.Startup;
using Api.Translate;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;


namespace Api.CustomContentTypes
{
	/// <summary>
	/// Generates system types from custom content type descriptions.
	/// </summary>
	public static class TypeEngine
    {
		/// <summary>
		/// Ensures unique names for assemblies generated during this session.
		/// </summary>
		private static int counter = 1;
		
		/// <summary>
		/// Tidy name suitable for fields or types.
		/// </summary>
		public static string TidyName(string str)
		{
			if (str == null)
				return null;
			
			str = str.Trim().Replace(" ", "");
			
			if (str.Length > 1)
				return char.ToUpper(str[0]) + str[1..];

			return str.ToUpper();
		}
		
		private static Dictionary<string, FieldType> GetTypeMap()
		{
			// All the system types supported by ContentSync as well as db storage.
			var map = new Dictionary<string, FieldType>();

			AddTo(
				map, "string", typeof(string), (ILGenerator code, string val) => {
				code.Emit(OpCodes.Ldstr, val);
			});

			AddTo(
				map, "jsonstring", typeof(string), (ILGenerator code, string val) => {
				code.Emit(OpCodes.Ldstr, val);
			});

			AddTo(
				map, "file", typeof(string), (ILGenerator code, string val) => {
				code.Emit(OpCodes.Ldstr, val);
			});

			AddTo(map, "long",typeof(long?), (ILGenerator code, string val) => {
				var value = long.TryParse(val, out long result) ? result : 0;
				code.Emit(OpCodes.Ldc_I8, value);
			});

			AddTo(map, "ulong", typeof(ulong?), (ILGenerator code, string val) => {
				var value = ulong.TryParse(val, out ulong result) ? result : 0;
				code.Emit(OpCodes.Ldc_I8, value);
			});

			AddTo(map, "int", typeof(int?), (ILGenerator code, string val) => {
				var value = int.TryParse(val, out int result) ? result : 0;
				code.Emit(OpCodes.Ldc_I4, value);
			});

			AddTo(map, "uint", typeof(uint?), (ILGenerator code, string val) => {
				var value = uint.TryParse(val, out uint result) ? result : 0;
				code.Emit(OpCodes.Ldc_I4, value);
			});

			AddTo(map, "entity", typeof(uint?), (ILGenerator code, string val) => {
				var value = uint.TryParse(val, out uint result) ? result : 0;
				code.Emit(OpCodes.Ldc_I4, value);
			});

			AddTo(map, "bool", typeof(bool?), (ILGenerator code, string val) => {
				var value = int.TryParse(val, out int result) ? result : 0;
				code.Emit(OpCodes.Ldc_I4, value);
			});

			AddTo(map, "datetime", typeof(DateTime), (ILGenerator code, string val) => {
				var value = int.TryParse(val, out int result) ? result : 0;
				code.Emit(OpCodes.Ldc_I4, value);
			});

			AddTo(map, "float", typeof(float?), (ILGenerator code, string val) => {
				var value = float.TryParse(val, out float result) ? result : 0;
				code.Emit(OpCodes.Ldc_R4, value);
			});

			AddTo(map, "double", typeof(double?), (ILGenerator code, string val) => {
				var value = double.TryParse(val, out double result) ? result : 0;
				code.Emit(OpCodes.Ldc_R8, value);
			});

			/*
			AddTo(map, "short", typeof(short), OpCodes.Ldc_I4, (string val) => short.TryParse(val, out short result) ? result : (short)0);
			AddTo(map, "ushort", typeof(ushort), OpCodes.Ldc_I4, (string val) => ushort.TryParse(val, out ushort result) ? result : (ushort)0);
			AddTo(map, "decimal", typeof(decimal), OpCodes.Ldc_I4, (string val) => int.TryParse(val, out int result) ? result : 0);
			AddTo(map, "byte", typeof(byte), OpCodes.Ldc_I4, (string val) => int.TryParse(val, out int result) ? result : 0);
			*/

			return map;
		}
		
		private static FieldType AddTo(Dictionary<string, FieldType> map, string lowercaseName, Type type, Action<ILGenerator, string> onDefault)
		{
			var fieldType = new FieldType(lowercaseName, type, onDefault);
			map[lowercaseName] = fieldType;
			return fieldType;
		}
		
		/// <summary>
		/// Gets the system type for the given textual name.
		/// </summary>
		private static FieldType GetFieldType(string typeName, Dictionary<string, FieldType> map){
			if(string.IsNullOrWhiteSpace(typeName))
			{
				return null;
			}
			
			typeName = typeName.ToLower().Trim();
			map.TryGetValue(typeName, out FieldType result);
			return result;
		}

		/// <summary>
		/// Generates a system type from the given custom type description.
		/// </summary>
		public static ConstructedCustomContentType Generate(CustomContentType type)
		{
			var set = new List<CustomContentType>
			{
				type
			};
			return Generate(set)[0];
		}

		/// <summary>
		/// Generates a system type from the given custom type descriptions.
		/// </summary>
		public static List<ConstructedCustomContentType> Generate(List<CustomContentType> types)
		{
			var builders = new List<UnconstructedCustomContentType>();
			var result = new List<ConstructedCustomContentType>();
			
			AssemblyName assemblyName = new AssemblyName("GeneratedTypes_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
			
			var typeMap = GetTypeMap();
			
			foreach(var customType in types)
			{
				if(customType == null)
				{
					continue;
				}

				// Base type to use:
				var baseType = typeof(VersionedContent<uint>);

				// Start building the type:
				var typeName = TidyName(customType.Name);
				TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public, baseType);
				
				// We'll need a constructor to set any default field values.
				ConstructorBuilder ctor0 = typeBuilder.DefineConstructor(
					MethodAttributes.Public,
					CallingConventions.Standard,
					Type.EmptyTypes
				);
				
				ILGenerator constructorBody = ctor0.GetILGenerator();

				if (customType.Fields == null)
                {
					customType.Fields = new List<CustomContentTypeField>();
				}

				// Every custom content type needs a name to identify it
				if (customType.Fields.FirstOrDefault(field => field.Name == "Name") == null)
                {
					customType.Fields.Insert(0, 
						new CustomContentTypeField
						{
							CustomContentTypeId = customType.Id,
							Name = "Name",
							NickName = "Name",
							DataType = "string"
						}
					);
                }
				
				if(customType.Fields != null){
					
					// Add each field
					foreach(var field in customType.Fields)
					{
						if(field == null || string.IsNullOrWhiteSpace(field.DataType))
						{
							continue;
						}

						var fieldName = TidyName(field.Name);
						var fieldTypeInfo = GetFieldType(field.DataType, typeMap);
						
						if(fieldTypeInfo == null)
						{
							continue;
						}
						
						FieldBuilder fieldBuilder = typeBuilder.DefineField(fieldName, fieldTypeInfo.Type, FieldAttributes.Public);

						// Add the nickname as an attribute
						if (!string.IsNullOrWhiteSpace(field.NickName))
                        {
							AddDataAttribute(fieldBuilder, "label", field.NickName);
						}

						if (field.Localised)
                        {
							ConstructorInfo localisedAttrClassCtorInfo = typeof(LocalizedAttribute).GetConstructor(Type.EmptyTypes);

							CustomAttributeBuilder myLABuilder = new CustomAttributeBuilder(
								localisedAttrClassCtorInfo,
								new object[] {}
							);

							fieldBuilder.SetCustomAttribute(myLABuilder);
						}

						// Add other attributes if needed
						if (field.DataType == "jsonstring" || field.DataType.ToLower() == "jsonstring")
                        {
							AddDataAttribute(fieldBuilder, "type", "canvas");
						} 
						else if (field.DataType == "file")
                        {
							AddDataAttribute(fieldBuilder, "type", "file");
						}
						else if (field.DataType == "entity" && !string.IsNullOrEmpty(field.LinkedEntity))
                        {
							AddDataAttribute(fieldBuilder, "contentType", TidyName(field.LinkedEntity));

							Type[] moduleAttrParams = new Type[1] { typeof(String) };

							ConstructorInfo moduleAttrClassCtorInfo = typeof(ModuleAttribute).GetConstructor(moduleAttrParams);

							CustomAttributeBuilder myModuleAttributeBuilder = new CustomAttributeBuilder(
								moduleAttrClassCtorInfo,
								new object[1] { "Admin/ContentSelect" }
							);

							fieldBuilder.SetCustomAttribute(myModuleAttributeBuilder);
						}

						if (!string.IsNullOrEmpty(field.DefaultValue))
						{
							// Set the default value for the field:
							constructorBody.Emit(OpCodes.Ldarg_0);
							fieldTypeInfo.EmitDefault(constructorBody, field.DefaultValue);
							constructorBody.Emit(OpCodes.Stfld, fieldBuilder);
						}
					}
				}
				
				constructorBody.Emit(OpCodes.Ret);

				// Create a controller type as well. This is because API route controllers require the [Route] attribute.

				var controllerBaseType = typeof(AutoController<>).MakeGenericType(new Type[] { typeBuilder });

				TypeBuilder controllerTypeBuilder = moduleBuilder.DefineType(typeName + "Controller", TypeAttributes.Public, controllerBaseType);

				var routeAttribute = typeof(Microsoft.AspNetCore.Mvc.RouteAttribute);

				var routeAttrCtor = routeAttribute.GetConstructor(new Type[] { typeof(string) });

				controllerTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(routeAttrCtor, new object[] { "v1/" + typeName.ToLower() }));

				builders.Add(new UnconstructedCustomContentType()
				{
					Id = customType.Id,
					Name = typeName,
					TypeBuilder = typeBuilder,
					ControllerTypeBuilder = controllerTypeBuilder,
					Fields = customType.Fields
				});
			}

			foreach (var builder in builders)
            {
				var linkedFields = builder.Fields.Where(field => (field.DataType == "entity" || field.DataType == "entitylist") && !string.IsNullOrEmpty(field.LinkedEntity));

				foreach (var linkedField in linkedFields)
                {
					if (linkedField.DataType == "entity")
                    {
						Type[] hvfAttrParams = new Type[3] { typeof(String), typeof(String), typeof(String) };

						ConstructorInfo hvfAttrClassCtorInfo = typeof(HasVirtualFieldAttribute).GetConstructor(hvfAttrParams);

						CustomAttributeBuilder myHVFAttributeBuilder = new CustomAttributeBuilder(
							hvfAttrClassCtorInfo,
							new object[3] { linkedField.LinkedEntity, linkedField.LinkedEntity, linkedField.Name }
						);

						builder.TypeBuilder.SetCustomAttribute(myHVFAttributeBuilder);
					} 
					else
                    {
						Type[] hvfAttrParams = new Type[3] { typeof(String), typeof(String), typeof(bool) };

						ConstructorInfo hvfAttrClassCtorInfo = typeof(HasVirtualFieldAttribute).GetConstructor(hvfAttrParams);

						CustomAttributeBuilder myHVFAttributeBuilder = new CustomAttributeBuilder(
							hvfAttrClassCtorInfo,
							new object[3] { linkedField.Name, linkedField.LinkedEntity, true }
						);

						builder.TypeBuilder.SetCustomAttribute(myHVFAttributeBuilder);
					}
				}

				// Finish the type.
				Type builtType = builder.TypeBuilder.CreateType();
				Type builtControllerType = builder.ControllerTypeBuilder.CreateType();
				result.Add(new ConstructedCustomContentType()
				{
					Id = builder.Id,
					ContentType = builtType,
					ControllerType = builtControllerType
				});
			}

			return result;
		}

		private static void AddDataAttribute(FieldBuilder fieldBuilder, string arg1, object arg2)
        {
			Type[] dataAttrParams = new Type[2] { typeof(String), typeof(Object) };

			ConstructorInfo dataAttrClassCtorInfo = typeof(DataAttribute).GetConstructor(dataAttrParams);

			CustomAttributeBuilder myCABuilder = new CustomAttributeBuilder(
				dataAttrClassCtorInfo,
				new object[2] { arg1, arg2 }
			);

			fieldBuilder.SetCustomAttribute(myCABuilder);
		}
	}

	/// <summary>
	/// A constructed custom content type.
	/// </summary>
	public class ConstructedCustomContentType
	{
		/// <summary>
		/// The ID of the CustomContentType.
		/// </summary>
		public uint Id;
		/// <summary>
		/// The underlying custom type.
		/// </summary>
		public Type ContentType;
		/// <summary>
		/// The controller type for this custom type.
		/// </summary>
		public Type ControllerType;
		/// <summary>
		/// The autoservice for this custom type.
		/// </summary>
		public AutoService Service;
	}

	class UnconstructedCustomContentType
    {
		public uint Id;

		public string Name;

		public TypeBuilder TypeBuilder;

		public TypeBuilder ControllerTypeBuilder;

		public List<CustomContentTypeField> Fields;
    }

}