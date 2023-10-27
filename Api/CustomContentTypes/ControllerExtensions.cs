using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Api.CustomContentTypes
{

	/// <summary>
	/// "Feature providers" is the mechanism used to declare additional controllers available inside MVC.
	/// This is used to add the runtime constructed controllers used by custom types.
	/// </summary>
	public class CustomTypeFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
	{

		/// <summary>
		/// Adds the custom controllers as they are currently.
		/// </summary>
		public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
		{
			var types = Services.Get<CustomContentTypeService>();

			if (types == null)
			{
				return;
			}
			
			// Get the controller set:
			var customTypeSet = types.GetGeneratedTypes();

			if (customTypeSet != null)
			{
				foreach (var customType in customTypeSet)
				{
					feature.Controllers.Add(IntrospectionExtensions.GetTypeInfo(customType.Value.ControllerType));
				}
			}
		}
	}

	/// <summary>
	/// A change provider which signals changes to action descriptions.
	/// </summary>
	public class ActionDescriptorChangeProvider : IActionDescriptorChangeProvider
	{
		/// <summary>
		/// Main instance of the change provider. Use this to signal changes.
		/// </summary>
		public static ActionDescriptorChangeProvider Instance { get; } = new ActionDescriptorChangeProvider();

		/// <summary>
		/// The underlying cancellation token src.
		/// </summary>
		public CancellationTokenSource TokenSource { get; private set; }

		/// <summary>
		/// Set true when it has changed.
		/// </summary>
		public bool HasChanged { get; set; }

		/// <summary>
		/// Change token used when action descriptions change.
		/// </summary>
		/// <returns></returns>
		public IChangeToken GetChangeToken()
		{
			TokenSource = new CancellationTokenSource();
			return new CancellationChangeToken(TokenSource.Token);
		}
	}

}