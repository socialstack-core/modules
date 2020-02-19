using Api.Projects;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		#region Service events
		
		/// <summary>
		/// Set of events for a Project.
		/// </summary>
		public static EventGroup<Project> Project;
		
		#endregion

		#region Controller events

		/// <summary>
		/// Create a new project.
		/// </summary>
		public static EndpointEventHandler<ProjectAutoForm> ProjectCreate;
		/// <summary>
		/// Delete an project.
		/// </summary>
		public static EndpointEventHandler<Project> ProjectDelete;
		/// <summary>
		/// Update project metadata.
		/// </summary>
		public static EndpointEventHandler<ProjectAutoForm> ProjectUpdate;
		/// <summary>
		/// Load project metadata.
		/// </summary>
		public static EndpointEventHandler<Project> ProjectLoad;
        /// <summary>
        /// List projects.
        /// </summary>
        public static EndpointEventHandler<Filter<Project>> ProjectList;

		#endregion

	}

}
