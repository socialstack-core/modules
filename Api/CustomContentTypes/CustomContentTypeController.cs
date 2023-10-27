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
        /// All custom types which will include deleted ones and forms.
        /// </summary>
        /// <returns></returns>
        [HttpGet("alltypes")]
        public async ValueTask<List<string>> GetAllTypes()
        {
            var types = Services
                .AllByName
                .Where(type => type.Value.ServicedType != null)
                .Select(type => type.Value.ServicedType.Name)
                .ToList();

            return types;
        }

        /// <summary>
        /// Gets all custom types excluding deleted or form ones.
        /// </summary>
        /// <returns></returns>
        [HttpGet("allcustomtypesplus")]
        public async ValueTask<List<TypeInfo>> GetAllTypesPlus()
        {
            var results = new List<TypeInfo>();
            
            var context = await Request.GetContext();

            var customTypes = await (_service as CustomContentTypeService).Where("Deleted=? AND IsForm=?", DataOptions.IgnorePermissions).Bind(false).Bind(false).ListAll(context);

            if (customTypes != null)
            {
                foreach(var customType in customTypes)
                {
                    results.Add(new TypeInfo(customType.NickName, customType.Name));
                }
            }

            var types = Services
                .AllByName
                .Where(type => type.Value.ServicedType != null)
                .Select(type => type.Value.ServicedType.Name)
                .ToList();

            if (types.Contains("Tag"))
            {
                results.Add(new TypeInfo("Tag", "Tag"));
            }

            return results;
        }

        /// <summary>
        /// Information about a type.
        /// </summary>
        public class TypeInfo 
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
            public TypeInfo(string name, string value)
            {
                Name = name;
                Value = value; 
            }
        }
    }
}