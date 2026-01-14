
using Api.Configuration;
using System.Collections.Generic;

namespace Api.SearchMetaData
{
	/// <summary>
	/// Config for search indexing service.
	/// </summary>
	public class SearchMetaDataConfig : Config
	{

        /*
          "Mappings": [
          {
            "Name" : "Product",
		    "FieldNames" : [
			    "Sku",
			    "Name",
			    "DescriptionJson"
			    ],
                "Includes" : [
			        {
				        "Name" : "attributes",
				        "FieldNames" : [
					        "Value"
				        ]
			        },
			        {
				        "Name" : "ProductCategories",
				        "FieldNames" : [
					        "Name"
				        ]
			        }
		        ]
            }
	    ]
        */


        /// <summary>
        /// List include mappings for content types
        /// </summary>		
        public List<MappingEntity> Mappings { get; set; }

        /// <summary>
        /// Should debug info be written to the console?
        /// </summary>
        public bool DebugToConsole { get; set; } = false;
    }


    /// <summary>
    /// Config item to store the mapping between a content type and the includes which should be indexed
    /// </summary>
    public class MappingEntity
    {
        /// <summary>
        /// The name of the content type e.g. User/Event etc
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of field names which can be extracted
        /// </summary>
        public List<string> FieldNames { get; set; }

        /// <summary>
        /// The includes for the content type
        /// </summary>
        public List<MappingInclude> Includes { get; set; }
    }

    /// <summary>
    /// Config item for defining which fields are included and indexed 
    /// </summary>
    public class MappingInclude
    {
        /// <summary>
        /// The name of the content type e.g. User/Event etc
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of field names which can be extracted
        /// </summary>
        public List<string> FieldNames { get; set; }
    }


}