using Api.Startup;
using System;
using System.Collections.Generic;

namespace Api.AvailableEndpoints;


/// <summary>
/// The list of endpoints in a particular module (controller).
/// </summary>
public class ModuleEndpoints
{
	
	/// <summary>
	/// The module's controller that these endpoints belong to.
	/// </summary>
	public Type ControllerType;
	
	/// <summary>
	/// The endpoints themselves.
	/// </summary>
	public List<Endpoint> Endpoints = new List<Endpoint>();

	/// <summary>
	/// Gets the AutoController type, if there is one. Null if this module does not use AutoController.
	/// </summary>
	/// <returns></returns>
	public Type GetAutoControllerType()
	{
		return Services.GetAutoControllerType(ControllerType);
	}

	/// <summary>
	/// Gets the content type associated with this module, if there is one. It can be null.
	/// </summary>
	/// <returns></returns>
	public Type GetContentType()
	{
		var autoControllerType = GetAutoControllerType();

		if (autoControllerType == null)
		{
			return null;
		}

		// It's an AutoController<T, ID>. The content type is the T, the first generic arg.
		var args = autoControllerType.GetGenericArguments();
		return args[0];
	}

	/// <summary>
	/// Gets the autoService associated with this module if there is one. 
	/// It can be null if it is not an autoService based controller.
	/// </summary>
	/// <returns></returns>
	public AutoService GetAutoService()
	{
		var cType = GetContentType();

		if (cType == null)
		{
			return null;
		}

		return Services.GetByContentType(cType);
	}

}