using System;
using System.Linq;
using Api.Eventing;
using Api.Startup;

namespace Api.Generation
{
    /// <summary>
    /// Generation service, adds support for generated files based off reflection.
    /// </summary>
    public partial class GenerationService : AutoService
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GenerationService()
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
                await Events.Generation.OnEntityFound.Dispatch(ctx, genericEntityType);
                await Events.Generation.OnControllerFound.Dispatch(ctx, matchingController);

                return service;

            });
        }
    }
}