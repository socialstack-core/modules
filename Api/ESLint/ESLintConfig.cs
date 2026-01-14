using Api.Configuration;


namespace Api.ESLint
{
	
	/// <summary>
	/// The configuration for ESLint
	/// </summary>
	public partial class ESLintConfig: Config
	{
		/// <summary>
		/// True if the ESLint module should be disabled.
		/// </summary>
		public bool Disabled {get; set;}
		
		// public bool RunOnCommit {get; set;}
	}

}