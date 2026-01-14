using Api.Contexts;
using Api.Database;
using Api.Startup;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Api.Payments;


/// <summary>
/// Generates a slug from a title.
/// </summary>
public static class SlugGenerator
{

	/// <summary>
	/// Used to generate a unique slug.
	/// </summary>
	/// <returns></returns>
	public static async ValueTask<string> GenerateUniqueSlug<T, ID>(AutoService<T, ID> service, Context ctx, string title)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		// Initial slug value:
		var baseSlug = SlugGenerator.CreateSlug(title);
		var slug = baseSlug;
		var num = 0;

		while (!await IsSlugUnique(service, ctx, slug))
		{
			num++;
			slug = baseSlug + "-" + num;
		}

		return slug;
	}

	/// <summary>
	/// Finds an entry by as-is slug.
	/// </summary>
	/// <param name="service"></param>
	/// <param name="context"></param>
	/// <param name="slug"></param>
	/// <returns></returns>
	private static async ValueTask<bool> IsSlugUnique<T, ID>(AutoService<T, ID> service, Context context, string slug)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		var existing = await service.Where("Slug=?", DataOptions.IgnorePermissions).Bind(slug).First(context);
		return existing == null;
	}

	/// <summary>
	/// Generates a slug from a title.
	/// </summary>
	public static string CreateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
		{
			throw new PublicException("A title is required", "title/required");
		}
		
        string slug = title.ToLowerInvariant();
		
        // Replace spaces with dashes
        slug = Regex.Replace(slug, @"\s+", "-");
		
        // Remove any character that is not a-z, A-Z, 0-9, or dash
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
		
        // Remove multiple consecutive dashes
        slug = Regex.Replace(slug, @"-+", "-");
		
        // Trim dashes from the start and end
        slug = slug.Trim('-');
		
        return slug;
    }
}