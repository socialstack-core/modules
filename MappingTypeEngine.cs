using Api.Database;
using Api.Eventing;
using Api.SocketServerLibrary;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.Startup
{
	/// <summary>
	/// Generates system types from mapping type descriptions.
	/// </summary>
	public static class MappingTypeEngine
    {
		/// <summary>
		/// Ensures unique names for assemblies generated during this session.
		/// </summary>
		private static int counter = 1;

		/// <summary>
		/// Generated mapper services.
		/// </summary>
		private static Dictionary<string, AutoService> _mappers = new Dictionary<string, AutoService>();

		/// <summary>
		/// Gets or generates a mapping from src to target.
		/// </summary>
		/// <param name="srcType"></param>
		/// <param name="targetType"></param>
		/// <param name="listAs"></param>
		/// <returns></returns>
		public static async ValueTask<AutoService> GetOrGenerate(AutoService srcType, AutoService targetType, string listAs)
		{
			var srcTypeName = srcType.ServicedType.Name;
			var targetTypeName = targetType.ServicedType.Name;
			var typeName = srcTypeName + "_" + targetTypeName + "_Map_" + listAs;

			if (_mappers.TryGetValue(typeName, out AutoService mapper))
			{
				return mapper;
			}

			mapper = await Generate(srcType, targetType, typeName);
			_mappers[typeName] = mapper;
			return mapper;
		}

		/// <summary>
		/// Generates a system type from the given custom type descriptions.
		/// </summary>
		private static async ValueTask<AutoService> Generate(AutoService srcType, AutoService targetType, string typeName)
		{
			AssemblyName assemblyName = new AssemblyName("GeneratedMappings_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

			// Base type to use:
			var baseType = typeof(Mapping<,>).MakeGenericType(new Type[] { srcType.IdType, targetType.IdType });

			// Start building the type:
			var srcTypeName = srcType.ServicedType.Name;
			var targetTypeName = targetType.ServicedType.Name;
			TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public, baseType);

			// Setup constructor:
			ConstructorBuilder ctor0 = typeBuilder.DefineConstructor(
				MethodAttributes.Public,
				CallingConventions.Standard,
				Type.EmptyTypes
			);

			ILGenerator constructorBody = ctor0.GetILGenerator();
			constructorBody.Emit(OpCodes.Ret);

			var srcIdName = srcTypeName + "Id";

			// Source ID:
			var srcField = typeBuilder.DefineField(srcIdName, srcType.IdType, FieldAttributes.Public);

			// Target ID:
			var targetField = typeBuilder.DefineField(targetTypeName + "Id", targetType.IdType, FieldAttributes.Public);

			// Create srcId property:
			var srcId = typeBuilder.DefineProperty("SourceId", PropertyAttributes.None, srcType.IdType, new Type[] { srcType.IdType });
			var getSrcId = typeBuilder.DefineMethod("get_SourceId", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.SpecialName, srcType.IdType, Array.Empty<Type>());

			var body = getSrcId.GetILGenerator();

			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, srcField);
			body.Emit(OpCodes.Ret);

			srcId.SetGetMethod(getSrcId);

			// Create targetId property:
			var targetId = typeBuilder.DefineProperty("TargetId", PropertyAttributes.None, targetType.IdType, new Type[] { targetType.IdType });
			var getTargetId = typeBuilder.DefineMethod("get_TargetId", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.SpecialName, targetType.IdType, Array.Empty<Type>());

			body = getTargetId.GetILGenerator();

			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, targetField);
			body.Emit(OpCodes.Ret);

			targetId.SetGetMethod(getTargetId);

			var writerType = typeof(Writer);

			var toJsonMethod = typeBuilder.DefineMethod("ToJson", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), new Type[] {
				writerType
			});

			body = toJsonMethod.GetILGenerator();

			var writeSUint = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(uint) });
			var writeByte = typeof(Writer).GetMethod("Write", new Type[] { typeof(byte) });
			var writeSUlong = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(ulong) });

			// ID (uint):
			var idField = baseType.GetField("Id");
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, idField);
			body.Emit(OpCodes.Callvirt, writeSUint);
			
			// Comma:
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Ldc_I4_S, (byte)',');
			body.Emit(OpCodes.Callvirt, writeByte);
			
			// Src (type either uint or ulong):
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, srcField);
			body.Emit(OpCodes.Callvirt, srcType.IdType == typeof(uint) ? writeSUint : writeSUlong);

			// Comma:
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Ldc_I4_S, (byte)',');
			body.Emit(OpCodes.Callvirt, writeByte);
			
			// Target (type either uint or ulong):
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, targetField);
			body.Emit(OpCodes.Callvirt, srcType.IdType == typeof(uint) ? writeSUint : writeSUlong);
			
			body.Emit(OpCodes.Ret);

			// Add the [DatabaseIndex] attrib such that it creates a fast non-unique index for SourceId.
			var dbiAttribute = typeof(DatabaseIndexAttribute);

			var dbiAttributeCtor = dbiAttribute.GetConstructor(new Type[] { typeof(bool), typeof(string) });

			typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(dbiAttributeCtor, new object[] { false, srcIdName }));

			// Finish the type.
			Type compiledType = typeBuilder.CreateType();

			var installMethod = typeof(MappingTypeEngine).GetMethod(nameof(InstallType));

			// Invoke InstallType:
			var setupType = installMethod.MakeGenericMethod(new Type[] {
				srcType.ServicedType,		// SRC
				targetType.ServicedType,	// TARG
				compiledType,				// T
				srcType.IdType,				// SRC_ID
				targetType.IdType			// TARG_ID
			});

			var svc = await (ValueTask<AutoService>)setupType.Invoke(null, new object[] { srcTypeName + "Id", targetTypeName + "Id" });
			return svc;
		}

		/// <summary>
		/// Creates a service etc for the given system type and activates it. Invoked via reflection with a runtime compiled type.
		/// </summary>
		public static async ValueTask<AutoService> InstallType<SRC,TARG,T, SRC_ID, TARG_ID>(string srcIdName, string targetIdName) 
			where T : Mapping<SRC_ID, TARG_ID>, new()
			where SRC : Content<SRC_ID>
			where TARG : Content<TARG_ID>
			where SRC_ID : struct, IEquatable<SRC_ID>
			where TARG_ID : struct
		{
			// Create the service:
			var svc = new MappingService<SRC, TARG, SRC_ID, TARG_ID>(srcIdName, targetIdName, typeof(T));

			// Register:
			await Services.StateChange(true, svc);

			return svc;
		}
	}

}