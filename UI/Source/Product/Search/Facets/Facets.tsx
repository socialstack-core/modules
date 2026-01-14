import { ProductCategory } from 'Api/ProductCategory';
import { ProductAttributeValue } from 'Api/ProductAttributeValue';
import { ProductAttribute } from 'Api/ProductAttribute';

export interface ProductPriceFacet {
	key: string;
	value: number;
}

export interface ProductCategoryFacet {
	count: int;
	category: ProductCategory
}

export interface AttributeValueFacet {
	count: int;
	value: ProductAttributeValue
}

export interface AttributeFacetGroup {
	attribute: ProductAttribute,
	facetValues: AttributeValueFacet[]
}
