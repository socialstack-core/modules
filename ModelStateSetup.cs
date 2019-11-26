using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;


namespace Api.AutoForms
{

	/// <summary>
	/// Adds a Setup extension method to ModelState.
	/// This is used to automatically deal with invalid models, as well as set the result object.
	/// </summary>
	public static class ModelStateSetup
	{

		/// <summary>
		/// Call this to setup your autoform state.
		/// </summary>
		public static bool Setup<T>(this ModelStateDictionary modelState, AutoForm<T> autoForm, T result)
		{
			if (autoForm == null || !modelState.IsValid)
			{
				// Output the modelstate errors so API users know what's up.
				return false;
			}
			
			if (result == null)
			{
				return false;
			}

			// Set initial result field:
			autoForm.Result = result;

			// Setup a ref to the model state dictionary so extensions can check 
			// if a field was even defined:
			autoForm.RawModelFields = modelState;

			// For all non-ignored fields in the AutoForm's actual type (because they inherit AutoForm<T>), copy their value now.
			var fieldModel = AutoFormFieldMap.Get(autoForm.GetType());
			
			for (var i = 0; i < fieldModel.FieldPairs.Length; i++)
			{
				var fieldPair = fieldModel.FieldPairs[i];

				#warning this unfortunately doesnt work - ContainsKey returns false for everything except the route id.
				// Only copy the value if it was actually defined.

				/*
				if (!modelState.ContainsKey(fieldPair.Name))
				{
					continue;
				}
				*/

				var value = fieldPair.Source.GetValue(autoForm);
				fieldPair.Target.SetValue(result, value);
			}

			return true;
		}

	}
}
