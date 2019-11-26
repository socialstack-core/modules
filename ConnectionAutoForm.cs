using Api.AutoForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.Connections
{
	/// <summary>
	/// Used when creating or updating a connection
	/// </summary>
	public partial class ConnectionAutoForm : AutoForm<Connection>
	{
		/// <summary>
		/// The user id this (creator) user is subscribed to.
		/// </summary>
		public int ConnectedToId;
		
		/// <summary>
		/// Close friend, friend, aquaintance, mother, father, spouse etc.
		/// </summary>
		public int ConnectionTypeId;
	}

}