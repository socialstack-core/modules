using Api.Configuration;
using Api.Database;
using Api.Eventing;
using Api.SocketServerLibrary;
using Api.Users;
using System;
using System.Collections.Concurrent;
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
		private static ConcurrentDictionary<string, MappingServiceGenerationMeta> _mappers = new ConcurrentDictionary<string, MappingServiceGenerationMeta>();

		/// <summary>
		/// Gets just the table name.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="target"></param>
		/// <param name="listAs"></param>
		/// <returns></returns>
		public static string GetTableName(Type src, Type target, string listAs)
		{
			var srcTypeName = src.Name;
			var targetTypeName = target.Name;
			var typeName = srcTypeName + "_" + targetTypeName + "_map_" + listAs;
			var name = AppSettings.DatabaseTablePrefix + typeName.ToLower();

			if (name.Length > 64)
			{
				name = name.Substring(0, 64);
			}

			return name;
		}
		
		private static object generationLock = new object();

		/// <summary>
		/// Gets or generates a mapping from src to target.
		/// </summary>
		/// <param name="srcType"></param>
		/// <param name="targetType"></param>
		/// <param name="listAs"></param>
		/// <param name="group">The database group into which the mapping should be added.</param>
		/// <returns></returns>
		public static async ValueTask<AutoService> GetOrGenerate(AutoService srcType, AutoService targetType, string listAs, string group = null)
		{
			var srcTypeName = srcType.ServicedType.Name;
			var targetTypeName = targetType.ServicedType.Name;
			var typeName = srcTypeName + "_" + targetTypeName + "_Map_" + listAs;

			if (_mappers.TryGetValue(typeName, out MappingServiceGenerationMeta meta))
			{
				if (meta.Service != null)
				{
					return meta.Service;
				}

				return await meta.GenTask;
			}

			lock (generationLock)
			{
				// Check again:
				if (!_mappers.TryGetValue(typeName, out meta))
				{
					// It definitely doesn't exist yet.
					// It is vital that a service is only generated once. Generating it repeatedly causes things 
					// like ContentSync to reference a different copy of the same service.
					meta = new MappingServiceGenerationMeta();
					meta.GenTask = Task.Run(async () => {
						var svc = await Generate(srcType, targetType, typeName, group);
						meta.Service = svc;
						return svc;
					});
					_mappers[typeName] = meta;
				}
			}

			// Either it just created it, or some other thread just created it.
			// We never clear genTask, so it's safe to await it like so:
			return await meta.GenTask;
		}

		/// <summary>
		/// Generates a system type from the given custom type descriptions.
		/// </summary>
		private static async ValueTask<AutoService> Generate(AutoService srcType, AutoService targetType, string typeName, string group)
		{
			Type srcServicedType = srcType.ServicedType;
			Type srcIdType = srcType.IdType;
			Type targetServicedType = targetType.ServicedType;
			Type targetIdType = targetType.IdType;

			AssemblyName assemblyName = new AssemblyName("GeneratedMappings_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

			// Base type to use:
			var baseType = typeof(Mapping<,>).MakeGenericType(new Type[] { srcIdType, targetIdType });

			// Start building the type:
			var srcTypeName = srcServicedType.Name;
			var targetTypeName = targetServicedType.Name;
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
			var srcField = typeBuilder.DefineField(srcIdName, srcIdType, FieldAttributes.Public);

			// Target ID:
			var targetField = typeBuilder.DefineField(targetTypeName + "Id", targetIdType, FieldAttributes.Public);

			// Create srcId property:
			var srcIdTypeArr = new Type[] { srcIdType };
			var srcId = typeBuilder.DefineProperty("SourceId", PropertyAttributes.None, srcIdType, srcIdTypeArr);
			
			// Get method:
			var getSrcId = typeBuilder.DefineMethod("get_SourceId", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.SpecialName, srcIdType, Array.Empty<Type>());

			var body = getSrcId.GetILGenerator();

			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, srcField);
			body.Emit(OpCodes.Ret);

			srcId.SetGetMethod(getSrcId);

			// Set method:
			var setSrcId = typeBuilder.DefineMethod("set_SourceId", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.SpecialName, typeof(void), srcIdTypeArr);

			body = setSrcId.GetILGenerator();

			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Stfld, srcField);
			body.Emit(OpCodes.Ret);

			srcId.SetSetMethod(setSrcId);


			// Create targetId property:
			var targetIdTypeArr = new Type[] { targetIdType };
			var targetId = typeBuilder.DefineProperty("TargetId", PropertyAttributes.None, targetIdType, targetIdTypeArr);
			
			// Get method:
			var getTargetId = typeBuilder.DefineMethod("get_TargetId", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.SpecialName, targetIdType, Array.Empty<Type>());

			body = getTargetId.GetILGenerator();

			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, targetField);
			body.Emit(OpCodes.Ret);

			targetId.SetGetMethod(getTargetId);

			// Set method:
			var setTargetId = typeBuilder.DefineMethod("set_TargetId", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.SpecialName, typeof(void), targetIdTypeArr);

			body = setTargetId.GetILGenerator();

			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Stfld, targetField);
			body.Emit(OpCodes.Ret);

			targetId.SetSetMethod(setTargetId);


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
			body.Emit(OpCodes.Callvirt, srcIdType == typeof(uint) ? writeSUint : writeSUlong);

			// Comma:
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Ldc_I4_S, (byte)',');
			body.Emit(OpCodes.Callvirt, writeByte);
			
			// Target (type either uint or ulong):
			body.Emit(OpCodes.Ldarg_1);
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, targetField);
			body.Emit(OpCodes.Callvirt, srcIdType == typeof(uint) ? writeSUint : writeSUlong);
			
			body.Emit(OpCodes.Ret);

			// Add the [DatabaseIndex] attrib such that it creates a fast non-unique index for SourceId.
			var dbiAttribute = typeof(DatabaseIndexAttribute);

			var dbiAttributeCtor = dbiAttribute.GetConstructor(new Type[] { typeof(bool), typeof(string) });

			typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(dbiAttributeCtor, new object[] { false, srcIdName }));

			if (group != null)
			{
				// Add a [DatabaseField(Group = value)] attribute declaring the group:
				var dbfAttribute = typeof(DatabaseFieldAttribute);

				var dbfAttributeCtor = dbfAttribute.GetConstructor(new Type[] { typeof(string) });

				typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(dbfAttributeCtor, new object[] { group }));
			}

			// Finish the type.
			Type compiledType = typeBuilder.CreateType();

			var installMethod = typeof(MappingTypeEngine).GetMethod(nameof(InstallType));

			// Invoke InstallType:
			var setupType = installMethod.MakeGenericMethod(new Type[] {
				srcServicedType,		// SRC
				targetServicedType,	// TARG
				compiledType,				// T
				srcIdType,				// SRC_ID
				targetIdType			// TARG_ID
			});

			var svc = await (ValueTask<AutoService>)setupType.Invoke(null, new object[] {
				srcType,
				targetType,
				srcTypeName + "Id",
				targetTypeName + "Id"
			});

			return svc;
		}

		/// <summary>
		/// Creates a service etc for the given system type and activates it. Invoked via reflection with a runtime compiled type.
		/// </summary>
		public static async ValueTask<AutoService> InstallType<SRC,TARG,T, SRC_ID, TARG_ID>(AutoService<SRC, SRC_ID> src, AutoService<TARG, TARG_ID> targ, string srcIdName, string targetIdName) 
			where T : Mapping<SRC_ID, TARG_ID>, new()
			where SRC : Content<SRC_ID>, new()
			where TARG : Content<TARG_ID>, new()
			where SRC_ID : struct, IEquatable<SRC_ID>, IConvertible, IComparable<SRC_ID>
			where TARG_ID : struct, IEquatable<TARG_ID>, IConvertible, IComparable<TARG_ID>
		{
			// Create the service:
			var svc = new MappingService<SRC, TARG, SRC_ID, TARG_ID>(src, targ, srcIdName, targetIdName, typeof(T));

			// Register:
			await Services.StateChange(true, svc);

			return svc;
		}
	}

	/// <summary>
	/// Generation meta used whilst a mapping service starts.
	/// </summary>
	public class MappingServiceGenerationMeta
	{
		/// <summary>
		/// The service
		/// </summary>
		public AutoService Service;

		/// <summary>
		/// The generation task. If not null, await it.
		/// </summary>
		public Task<AutoService> GenTask;
	}

}