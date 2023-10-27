using Api.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Startup
{
    /// <summary>
    /// Generates system types from mapping type descriptions.
    /// </summary>
    public static partial class MappingTypeEngine
    {
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

            if (srcType.GeneratedMappings != null)
            {
                var mappedService = srcType.GeneratedMappings.FirstOrDefault(gm => gm.Target == targetType && gm.ListAs == listAs);

                if (mappedService != null)
                {
                    if (mappedService.Service != null)
                    {
                        return mappedService.Service;
                    }

                    return await mappedService.GenTask;
                }
            }

            MappingServiceGenerationMeta meta = null;

            lock (generationLock)
            {
                if (srcType.GeneratedMappings == null)
                {
                    srcType.GeneratedMappings = new List<MappingServiceGenerationMeta>();
                }
                else
                {
                    // Check again:
                     meta = srcType.GeneratedMappings.FirstOrDefault(gm => gm.Target == targetType && gm.ListAs == listAs);
                }

                if (meta == null)
                {
                    // It definitely doesn't exist yet.
                    // It is vital that a service is only generated once. Generating it repeatedly causes things 
                    // like ContentSync to reference a different copy of the same service.
                    meta = new MappingServiceGenerationMeta()
                    {
                        ListAs = listAs,
                        Target = targetType
                    };

                    meta.GenTask = Task.Run(async () =>
                        {
                            var svc = await Generate(srcType, targetType, typeName, group);
                            meta.Service = svc;
                            return svc;
                        });


                    srcType.GeneratedMappings.Add(meta);
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

            var installMethod = typeof(MappingTypeEngine).GetMethod(nameof(InstallType));

            // Invoke InstallType:
            var setupType = installMethod.MakeGenericMethod(new Type[] {
                srcServicedType,		// SRC
				targetServicedType,	// TARG
				srcIdType,				// SRC_ID
				targetIdType			// TARG_ID
			});

            var svc = await (ValueTask<AutoService>)setupType.Invoke(null, new object[] {
                srcType,
                targetType,
                typeName
            });

            return svc;
        }

        /// <summary>
        /// Creates a service etc for the given system type and activates it. Invoked via reflection with a runtime compiled type.
        /// </summary>
        public static async ValueTask<AutoService> InstallType<SRC, TARG, SRC_ID, TARG_ID>(AutoService<SRC, SRC_ID> src, AutoService<TARG, TARG_ID> targ, string name)
            where SRC : Content<SRC_ID>, new()
            where TARG : Content<TARG_ID>, new()
            where SRC_ID : struct, IEquatable<SRC_ID>, IConvertible, IComparable<SRC_ID>
            where TARG_ID : struct, IEquatable<TARG_ID>, IConvertible, IComparable<TARG_ID>
        {
            // Create the service:
            var svc = new MappingService<SRC, TARG, SRC_ID, TARG_ID>(src, targ, typeof(Mapping<SRC_ID, TARG_ID>), name);

            // Register:
            await Services.StateChange(true, svc);

            return svc;
        }

        /// <summary>
        /// When target Services are removed ensure all mapping are cleared 
        /// </summary>
        /// <param name="srcType"></param>
        /// <param name="targetType"></param>
        public static void Remove(AutoService srcType, AutoService targetType)
        {

            lock (generationLock)
            {
                if(srcType.GeneratedMappings == null)
                {
                    return;
                }

                srcType.GeneratedMappings = srcType.GeneratedMappings.Where(gm => gm.Target != targetType).ToList();
            }
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
        /// The mapping name
        /// </summary>
        public string ListAs;
                
        /// <summary>
        /// The target service
        /// </summary>
        public AutoService Target;

        /// <summary>
        /// The generation task. If not null, await it.
        /// </summary>
        public Task<AutoService> GenTask;
    }

}