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
		/// Just before a new project is created. The given project won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Project> ProjectBeforeCreate;

		/// <summary>
		/// Just after an project has been created. The given project object will now have an ID.
		/// </summary>
		public static EventHandler<Project> ProjectAfterCreate;

		/// <summary>
		/// Just before an project is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Project> ProjectBeforeDelete;

		/// <summary>
		/// Just after an project has been deleted.
		/// </summary>
		public static EventHandler<Project> ProjectAfterDelete;

		/// <summary>
		/// Just before updating an project. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Project> ProjectBeforeUpdate;

		/// <summary>
		/// Just after updating an project.
		/// </summary>
		public static EventHandler<Project> ProjectAfterUpdate;

		/// <summary>
		/// Just after an project was loaded.
		/// </summary>
		public static EventHandler<Project> ProjectAfterLoad;

		/// <summary>
		/// Just before a service loads an project list.
		/// </summary>
		public static EventHandler<Filter<Project>> ProjectBeforeList;

		/// <summary>
		/// Just after an project list was loaded.
		/// </summary>
		public static EventHandler<List<Project>> ProjectAfterList;

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
