import { useSession } from 'UI/Session';
import { useState } from "react";
import productCategoryApi, { ProductCategory } from 'Api/ProductCategory';
import businessApi, { Business } from 'Api/Business';
import useApi from "UI/Functions/UseApi";
import List from 'UI/ProductCategory/List';
import Breadcrumb from 'UI/Breadcrumb';

/**
 * Props for the Landing component.
 */
interface LandingProps {
    productCategory?: ProductCategory
}

/**
 * The website product category landing page React component.
 * @param props React props.
 */
const Landing: React.FC<LandingProps> = (props) => {

	const { 
		productCategory
	} = props;


	if (!productCategory)
	{
		return(``);
	}

	const [productSubCategories, setProductSubCategories] = useState<ProductCategory[]>();

	const { session } = useSession();
	var { user, role } = session;
	
	useApi(() => {
		var query = 'ParentId=?';
		var args: (number | number[])[] = [Number(productCategory.id)];

		return productCategoryApi.list({
			query,
			args,
			pageSize: 50 as int,
			pageIndex: 0 as int,
			sort: {
				field: "slug",
				direction: "asc"
			}
		}, [
			productCategoryApi.includes!.primaryurl
		]).then(results => {
			setProductSubCategories(results.results);
		});
	}, [productCategory.id]);
	
	const breadcrumbs = productCategory ? [
		{
			name: 'Home',
			href: '/'
		}, 
		...(productCategory.breadcrumb ?? []).map(breadcrumb => {
			return ({
				name: breadcrumb.name,
				href: breadcrumb.primaryUrl
			})
		})
	]: [];

	return (
		<>
			{productCategory && <>
				<Breadcrumb crumbs={breadcrumbs} />
			</>}

			{productSubCategories && productSubCategories.length > 0 && 
				<div className="product-category-landing">
					<List content={productSubCategories} />
				</div>
			}
		</>
	);
}

export default Landing;