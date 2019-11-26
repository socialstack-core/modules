using System;


namespace Api.Configuration
{
	/// <summary>
	/// Used to determine which environment we're in - production, staging or dev.
	/// Really just a wrapper around the ASPNETCORE_ENVIRONMENT environment variable.
	/// </summary>
	public static class Environment
	{
		/// <summary>
		/// The ASPNETCORE_ENVIRONMENT environment variable as a fast readonly string.
		/// </summary>
		public static readonly string Name;
		
		
		static Environment()
		{
			// Get the name:
			Name = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
		}
		
		/// <summary>
		/// True if this is the production environment.
		/// </summary>
		public static bool IsProduction()
		{
			return Name == Microsoft.AspNetCore.Hosting.EnvironmentName.Production;
		}
		
		/// <summary>
		/// True if this is the staging environment.
		/// </summary>
		public static bool IsStaging()
		{
			return Name == Microsoft.AspNetCore.Hosting.EnvironmentName.Staging;
		}
		
		/// <summary>
		/// True if this is the dev environment.
		/// </summary>
		public static bool IsDevelopment()
		{
			return Name == Microsoft.AspNetCore.Hosting.EnvironmentName.Development;
		}
		
	}
	
}