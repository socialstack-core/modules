using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Categories;

namespace Api.Articles
{

	/// <summary>
	/// An article, typically used in e.g. help guides or knowledge bases.
	/// </summary>
	public partial class Article : IHaveCategories
	{
		public List<Category> Categories { get; set; }
	}
}
