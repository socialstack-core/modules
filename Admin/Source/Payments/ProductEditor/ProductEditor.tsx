import { useEffect, useState } from "react";
import AutoForm from 'Admin/AutoForm';
import Link from 'UI/Link';
import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import { Product } from 'Api/Product';
import { useRouter } from 'UI/Router';
import productTemplateApi, { ProductTemplate } from 'Api/ProductTemplate';
import productCategoryApi, { ProductCategory } from 'Api/ProductCategory';
import { PublicError } from "UI/Failed";
import { CanvasNode } from "UI/Functions/CanvasExpand";
import { ProductAttribute } from "Api/ProductAttribute";
import { ProductAttributeValue } from "Api/ProductAttributeValue";

type ProductEditorProps = {
	content?: Product,
	singular: string,
	plural: string,
	contentType: string
};

function getRequiredAttributes(categories?:ProductCategory[], template?: ProductTemplate) {
	const requiredAttribs = [] as ProductAttribute[];

	if (categories) {
		categories.forEach(pc => {
			if (pc?.requiredAttributes?.length) {
				const ra = pc?.requiredAttributes;
				if (ra) {
					requiredAttribs.push(...ra);
				}
			}
		});
	}

	if (template) {
		const tra = template?.requiredAttributes;
		if (tra) {
			for (var i = 0; i < tra.length; i++) {
				var currentAttrib = tra[i];

				if (!requiredAttribs.find(attr => attr.id == currentAttrib.id)) {
					requiredAttribs.push(currentAttrib);
				}
			}
		}
	}

	return requiredAttribs;
}

const ProductEditor: React.FC<ProductEditorProps> = (props) => {
	let { content } = props;
	const { pageState } = useRouter();
	const { query } = pageState;
	const templateId = query?.get("template");
	const categoryId = query?.get("category");
	const [loadedTemplate, setLoadedTemplate] = useState<ProductTemplate | undefined>(content?.productTemplate);
	const [loadedCategory, setLoadedCategory] = useState<ProductCategory>();
	const [templateError, setTemplateError] = useState<PublicError | undefined>();
	const [initialContent, setInitialContent] = useState<Product | undefined>(props.content);
	const [attributes, setAttributes] = useState<ProductAttributeValue[] | undefined>();
	const [requiredAttributes, setRequiredAttributes] = useState<ProductAttribute[] | undefined>(() => {
		return getRequiredAttributes(content?.productCategories, content?.productTemplate);
	});

	useEffect(() => {
		if (!templateId) {
			return;
		}

		if (!loadedTemplate || loadedTemplate.id.toString() != templateId) {
			productTemplateApi
				.load(parseInt(templateId) as uint, [productTemplateApi.includes.requiredattributes])
				.then(template => {
					setTemplateError(undefined);
					setLoadedTemplate(template);
				})
				.catch(tpError => {
					setTemplateError(tpError);
				});
		}
	}, [templateId, loadedTemplate]);

	useEffect(() => {
		if (!categoryId) {
			return;
		}
		
		productCategoryApi
			.load(parseInt(categoryId) as uint, [productCategoryApi.includes.requiredattributes])
			.then(cat => {
				setTemplateError(undefined);
				setLoadedCategory(cat);
			})
			.catch(tpError => {
				setTemplateError(tpError);
			});
	}, [categoryId]);

	useEffect(() => {
		if (!((loadedCategory || !categoryId) && loadedTemplate) || content?.id) {
			return;
		}

		const initialContent: Product = { ...(loadedTemplate as Product), id: 0 as uint };

		if (loadedTemplate) {
			// Set the template ID in use:
			initialContent.productTemplateId = loadedTemplate.id;
		}

		if (loadedCategory) {
			// The provided category ID will be the only
			// one used in the spawned product.
			// This might have unwanted side effects so check accordingly!
			initialContent.productCategories = [loadedCategory];
		}

		setInitialContent(initialContent);
		setRequiredAttributes(getRequiredAttributes(initialContent.productCategories, loadedTemplate));

	}, [loadedCategory, loadedTemplate, content, categoryId]);

	useEffect(() => {
		if (!(content?.id)) {
			return;
		}

		setRequiredAttributes(getRequiredAttributes(content.productCategories, loadedTemplate));

	}, [loadedTemplate, content]);

	if (templateError) {
		return <Alert type='error'>
			{templateError.message || `Unable to load the template with ID '#${templateId}'`}
		</Alert>
	}

	if (templateId && !initialContent) {
		return <Loading />;
	}

	let rootRequiredAttribs = requiredAttributes;
	let childRequiredAttribs: ProductAttribute[] | undefined;

	const ctn = initialContent || content;

	if (ctn && ctn.productType == 2) {

		// If there are any required attributes, remove any that are 
		// present on this root variant product. The remaining required attributes are then
		// given to child variants only.
		const currentAttribs = attributes || ctn.attributes || [];

		rootRequiredAttribs = [] as ProductAttribute[];
		childRequiredAttribs = [] as ProductAttribute[];

		if (requiredAttributes) {
			requiredAttributes.forEach(attrib => {
				// Split them in to either the root or child sets.
				// Health warning: currentAttribs is an attribute value set.
				const isRoot = currentAttribs.find(attr => attr.productAttributeId == attrib.id);

				if (isRoot) {
					rootRequiredAttribs!.push(attrib);
				} else {
					childRequiredAttribs!.push(attrib);
				}
			});
		}
	}

	// We re-pass content such that it can be loaded from the template.
	return <AutoForm {...props} content={ctn} onChange={(newContent: Record<string, any>) => {
		setInitialContent({ ...newContent } as Product);
	}} onBeforeForm={() => {
		const variantOfId = content?.variantOfId;
		
		if(!variantOfId){
			return null;
		}
		
		return <p>
			<Link href={'/en-admin/product/' + variantOfId}>{`Edit parent product`}</Link>
		</p>;
		
	}} onRenderField={(contentNode: CanvasNode, content: Record<string, any>, isEdit: boolean) => {
		const name = contentNode.props?.name;


		// hide parentId field if variant
		if (name == 'parentId' && content?.productType == 2) {
			return false;
		}

		if (name == 'attributes') {
			// Categories forming any required attributes?
			contentNode.props!.requiredAttributes = rootRequiredAttribs;

			contentNode.props!.onRawChange = (e: any) => {
				setAttributes(e.fullValue);
			};
		}

		if (name == 'productCategories') {
			contentNode.props!.onRawChange = (e : any) => {
				setRequiredAttributes(getRequiredAttributes(e.fullValue, loadedTemplate));
			};
			contentNode.props!.includes = ['requiredAttributes'];
		}

		if (name == 'variants') {
			contentNode.props!.requiredAttributes = childRequiredAttribs;
		}

		// hide variants field if productType is not variant
		if(name == 'variants' && content?.productType != 2){
			return false;
		}
		
		// Hide attributes field on variant products
		if(content?.variantOfId){
			if(name == 'attributes'){
				return false;
			}
		}else{
			// Hide "additional attributes" for non-variant products.
			if(name == 'additionalAttributes'){
				return false;
			}
		}
		
		return true;
	}}/>;
};

export default ProductEditor;
