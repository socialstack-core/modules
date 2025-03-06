using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;

namespace Api.GenerateDevFiles
{
    /// <summary>
    /// Generation service, adds support for generated files based off reflection.
    /// </summary>
    public partial class DevFileGenerationService : AutoService
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DevFileGenerationService()
        {
            Events.Service.AfterCreate.AddEventListener(async (ctx, service) => {

                var svcType = service.GetType();

                // Get the base type (which should be AutoService<T>)
                var baseType = svcType.BaseType;

                // Check that the base type is generic and derived from AutoService<>
                if (baseType == null || !baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(AutoService<>))
                {
                    return service;
                }

                // Extract the entity type
                var genericEntityType = baseType.GetGenericArguments().FirstOrDefault();
                if (genericEntityType == null)
                {
                    return service;
                }

                var assembly = svcType.Assembly;

                var controllers = assembly.GetTypes()
                    .Where(t =>
                        t.IsClass &&
                        !t.IsAbstract &&
                        t.BaseType != null &&
                        t.BaseType.IsGenericType &&
                        t.BaseType.GetGenericTypeDefinition() == typeof(AutoController<>)
                    )
                    .ToList();

                // Find the matching controller for this entity
                var matchingController = controllers
                    .FirstOrDefault(controllerType =>
                    {
                        var controllerBaseType = controllerType.BaseType;
                        if (controllerBaseType == null || !controllerBaseType.IsGenericType)
                        {
                            return false;
                        }

                        var controllerEntityType = controllerBaseType.GetGenericArguments().FirstOrDefault();
                        return controllerEntityType == genericEntityType;
                    });

                if (matchingController is null)
                {
                    return service;
                }

                // Fire events now that entity and controller are both known
                await Events.DevFileGeneration.OnEntityFound.Dispatch(ctx, genericEntityType);
                await Events.DevFileGeneration.OnControllerFound.Dispatch(ctx, matchingController, genericEntityType);

                return service;

            });

            Events.Service.AfterStart.AddEventListener(async (ctx, svc) => {

                var controllersWithoutGenericArguments = svc.GetType().Assembly.GetTypes()
                    .Where(t =>
                        t.IsClass &&
                        !t.IsAbstract &&
                        t.BaseType != null &&
                        t.BaseType == typeof(ControllerBase) // Directly inherits from non-generic AutoController
                    )
                    .ToList();
                
                foreach(var controller in controllersWithoutGenericArguments)
                {
                    await Events.DevFileGeneration.OnControllerFound.Dispatch(ctx, controller, null);
                }


                return svc;
            });
        }
    }
}