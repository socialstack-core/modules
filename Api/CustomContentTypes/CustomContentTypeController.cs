using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.CustomContentTypes
{
    /// <summary>Handles customContentType endpoints.</summary>
    [Route("v1/customContentType")]
	public partial class CustomContentTypeController : AutoController<CustomContentType>
    {
        /// <summary>
        /// All custom types which will include deleted ones.
        /// </summary>
        /// <returns></returns>
        [HttpGet("alltypes")]
        public List<string> GetAllTypes()
        {
            var types = Services
                .AllByName
                .Where(type => type.Value.ServicedType != null)
                .Select(type => type.Value.ServicedType.Name)
                .ToList();

            return types;
        }

        /// <summary>
        /// Gets all custom types excluding deleted ones.
        /// </summary>
        /// <returns></returns>
        [HttpGet("allcustomtypesplus")]
        public async ValueTask<List<CustomTypeInfo>> GetAllTypesPlus(Context context)
        {
            var results = new List<CustomTypeInfo>();
            
            var customTypes = await (_service as CustomContentTypeService).Where("Deleted=?", DataOptions.IgnorePermissions).Bind(false).ListAll(context);

            if (customTypes != null)
            {
                foreach(var customType in customTypes)
                {
                    results.Add(new CustomTypeInfo(customType.NickName, customType.Name));
                }
            }

            var types = Services
                .AllByName
                .Where(type => type.Value.ServicedType != null)
                .Select(type => type.Value.ServicedType.Name)
                .ToList();

            if (types.Contains("Tag"))
            {
                results.Add(new CustomTypeInfo("Tag", "Tag"));
            }

            return results;
        }

        /// <summary>
        /// Information about a type.
        /// </summary>
        public class CustomTypeInfo 
        {
            /// <summary>
            /// The name of the type.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The value for the type.
            /// </summary>
            public string Value { get; set;}

            /// <summary>
            /// Create a TypeInfo with the type name and value.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            public CustomTypeInfo(string name, string value)
            {
                Name = name;
                Value = value; 
            }
        }
    }
}