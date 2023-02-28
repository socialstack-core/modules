
using Api.Configuration;
using System.Collections.Generic;

namespace Api.Translate
{
	/// <summary>
	/// Config for translation service.
	/// </summary>
	public class SearchElasticServiceConfig : Config
	{
        /// <summary>
        /// The elastic search instance user name
        /// </summary>
        public string UserName { get; set; } = "elastic";

        /// <summary>
        /// The elastic search instance user password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The elastic search instance fingerprint
        /// 
        /// How to get the client fingerprint 
        /// https://www.elastic.co/guide/en/elasticsearch/reference/8.1/configuring-stack-security.html#_connect_clients_to_elasticsearch_5
        /// 
        /// </summary>
        public string FingerPrint { get; set; }

        /// <summary>
        /// The url for connecting to the elastic search instance
        /// </summary>
        public string InstanceUrl { get; set; } = "";

        /// <summary>
        /// The index name 
        /// </summary>
        public string IndexName { get; set; } = "";

        /// <summary>
        /// List of tags to be treated as headers
        /// </summary>		
        public List<string> HeaderTags { get; set; } = new List<string>(){"h1", "h2", "h3", "h4", "h5", "h6"};

    }

}