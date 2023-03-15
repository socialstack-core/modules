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

        public class TypeInfo 
        {
            public string Name { get; set; }

            public string Value { get; set;}

            public TypeInfo(string name, string value)
            {
                Name = name;
                Value = value; 
            }
        }
    }
}