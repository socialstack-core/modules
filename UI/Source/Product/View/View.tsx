import { Product } from 'Api/Product';
import { PriceCurrency } from 'Api/Payments';
import ProductList from 'UI/Product/List';
import ProductCarousel, { CarouselItem } from 'UI/Product/Carousel';
import ProductAbout from 'UI/Product/About';
import ProductAttributes from 'UI/Product/Attributes';
import ProductPrice, { CurrencyAmount } from 'UI/Product/Price';
import ProductQuantity from 'UI/Product/Quantity';
import ProductDownloads from 'UI/Product/Downloads';
import ProductFAQs from 'UI/Product/FAQs';
import { useEffect, useState, useRef } from 'react';
import Breadcrumb, { Crumb } from 'UI/Breadcrumb';
import ProductHeader from 'UI/Product/Header';
import { useRouter } from 'UI/Router';
import Button from 'UI/Button';
import ProductVariants from 'UI/Product/Variants';
import Tabs from 'UI/Tabs';
import { Upload } from "Api/Upload";
import PromotionCycler, {InlinePromotion} from 'UI/PromotionCycler';

const ROOT_CATEGORY_ID: uint = 1 as uint;

/**
 * Props for the View component.
 */
interface ViewProps {
	/**
	 * Must have included at least productCategories, firstCategory, firstCategory.categoryBreadcrumb
	 * Usually provided by a graph.
	 */
	product: Product,

	/**
	 * Promotions array (injected by PromotionBodyInjectionEventListener)
	 */
	promotions?: InlinePromotion[],

	additionalActions?: (string | React.FC | React.FC<AdditionalActionProps>)[],
}

type AdditionalActionProps = {
	product: Product,
}

/**
 * The View React component.
 * @param props React props.
 */
const View: React.FC<ViewProps> = (props) => {
	const { product, promotions } = props;

	const { pageState, setPage, updateQuery } = useRouter();
	const { query, url } = pageState;

	// Must use a ref for updateQuery to avoid capturing router state
	const updateQueryRef = useRef(updateQuery);
	updateQueryRef.current = updateQuery;

	// Selected variant (if any) is..
	const variantSku = query?.get("sku");
	const currentVariant: Product | undefined = variantSku ? product?.variants?.find(prod => prod.sku == variantSku) : undefined;
	
	const [selectedThumbnail, setSelectedThumbnail] = useState<CarouselItem | Product>();

	useEffect(() => {
		// here we reset the selected thumbnail and load the variant one in.
		setSelectedThumbnail((prev) => currentVariant ?? prev);
	}, [variantSku, currentVariant]);

	if (!product) {
		return;
	}

	const downloadsSource = currentVariant || product;
	const downloads = downloadsSource?.productDownloads?.filter((download: Upload) => download.ref);

	enum ProductTab {
		About = `About This Product`,
		Details = `Details`,
		Downloads = `Downloads`
	}

	function getTabs(): ProductTab[] {
		const tabs: ProductTab[] = [];

		for (const tab of Object.values(ProductTab)) {

			if (showTab(tab)) {
				tabs.push(tab);
			}

		}

		return tabs;
	}

	const showTab = (tab: ProductTab) => {

		switch (tab) {
			// only include about tab if we either have a description to display
			case ProductTab.About:
				const source = currentVariant || product;
				const about = source?.descriptionHtml;
				
				return about?.length;

			// only include details tab if we have attributes to display
			case ProductTab.Details:
				let { attributes } = product;

				if (currentVariant?.attributes?.length) {
					attributes = currentVariant.attributes;
				}

				return attributes?.length;

			case ProductTab.Downloads:
				return downloads?.length;

		}

		return false;
	};

	const renderTab = (tab: string) => {

		switch (tab) {
			case ProductTab.About:
				return <>
					<div className="ui-product-view__tab--about">
						<ProductAbout product={product} currentVariant={currentVariant} />

						<ProductFAQs title={`Frequently Asked Questions`} product={product} currentVariant={currentVariant} />
					</div>
				</>;


			case ProductTab.Details:
				return <ProductAttributes product={product} currentVariant={currentVariant} />;

			case ProductTab.Downloads:
				return <ProductDownloads product={product} currentVariant={currentVariant} />;

			default:
				return;
		}

	}

	const productTabs = Object.values(getTabs());
	var defaultTab = '';

	for (const tab of Object.values(ProductTab)) {

		if (showTab(tab)) {
			defaultTab = tab;
			break;
		}

	}

	const currentTab = (query?.get("tab") || defaultTab).toLowerCase();

	const setCurrentTab = (target: string) => {
		updateQueryRef.current({ tab: target });
	};

	// variant checks
	const hasVariants = (product.variants ?? [])?.length > 0;

	// Get the base price for non-variant products
	var basePrice: PriceCurrency | null = null;
	if (!hasVariants && product?.calculatedPrice) {
		var calculatedPrice = product.calculatedPrice;
		var tiers = null;
		if (calculatedPrice.discountedPrice?.length > 0) {
			tiers = calculatedPrice.discountedPrice;
		} else if (calculatedPrice.listPrice?.length > 0) {
			tiers = calculatedPrice.listPrice;
		}
		// The highest tier is always the cheapest per-unit price
		if (tiers && tiers.length > 0) {
			basePrice = tiers[tiers.length - 1];
		}
	}

	var sortedPrices = hasVariants ? (product.variants ?? []).map(product => {
		var tiers = null;

		if (product?.calculatedPrice) {
			var calculatedPrice = product.calculatedPrice;

			if (calculatedPrice.discountedPrice?.length > 0) {
				tiers = calculatedPrice.discountedPrice;
			} else if (calculatedPrice.listPrice?.length > 0) {
				tiers = calculatedPrice.listPrice;
			}
		}
		if (!tiers || tiers.length === 0) {
			return null;
		}

		// The highest tier is always the cheapest per-unit price
		return tiers[tiers.length - 1];
	})
		.filter(price => !!price) // strip the nulls
		.sort((a, b) => a.amount - b.amount) : null;

	var cheapestPrice = sortedPrices?.length ? sortedPrices[0] : basePrice;

	// Added the required home breadcrumb as well as  
	// overwrite of the root categories name to "All products"
	return <>
		<div className="ui-product-view">
			{/* breadcrumb links */}
			{product.breadcrumb && <>
				<Breadcrumb crumbs={[
					{ name: `Home`, href: '/' },
					...product.breadcrumb.map(crumb => {
						return {
							name: crumb.id === ROOT_CATEGORY_ID ? `All Products` : crumb.name,
							href: crumb.primaryUrl
						} as Crumb;
					})
				]}
					includeCurrent
					currentLabel={product?.name ?? undefined}
				/>
			</>}

			{/* product images */}
			<ProductCarousel
				product={product}
				currentVariant={currentVariant}
				// this holds the current selected thumbnail from
				// the useState above, this component tells
				// the product carousel what to render
				// a user can select a thumbnail
				// different to the one from the attribute matrix
				// thus we hold it as a separate state item. 
				// when the attribute matrix is mutated, it
				// should clear the selected thumbnail
				selectedThumbnail={selectedThumbnail}

				// little subscriber to listen to when the thumb
				// changes, this doesn't change the selected product
				// discovered by the attribute matrix, this is just
				// for looking at the different variants.
			onThumbSelected={(thumbInfo: Product | CarouselItem) => {
					setSelectedThumbnail(thumbInfo);
				}}
			/>

			{/* promotions cycler */}
			{promotions && promotions.length > 0 && <PromotionCycler promotions={promotions} currentProductId={product.id} currentProductPriceInPence={cheapestPrice?.amount} />}

			{/* featured / title / stock info */}
			<ProductHeader product={product} currentVariant={currentVariant} />

			{/* tabs */}
			<Tabs currentTab={currentTab} tabs={productTabs} renderPanel={renderTab} onChange={(tab) => setCurrentTab(tab.toLowerCase())} />

			{/* price info */}
			<div className="ui-product-view__price-info">

				{/* product variants */}
				{hasVariants && <>
					<Button sm variant="primary" outlined className="ui-product-view__price-info-variants" popoverTarget="product_variants">
						<i className="fr fr-tasks"></i>
						<span>
							{`Options`}
						</span>
					</Button>
					<div id="product_variants" popover="auto">
						<div className="ui-product-view__price-info-variants-header">
							<span>
								{`Please select options`}
							</span>
						</div>
						<ProductVariants product={product} currentVariant={currentVariant} onChange={variant => {

							// Change the URL if needed. This triggers a re-render at this upper level which then ultimately
							// collects the variant and anything else necessary.
							// This way it is driven by url state and also pretty minimal, 
							// ensuring that the selected product is shareable.
							if (variant?.sku == variantSku) {
								return;
							}

							var nextQuery = new URLSearchParams(query);
							if (variant) {
								nextQuery.set("sku", variant.sku || '');
							} else {
								nextQuery.delete("sku");
							}

							const currentUrl = url.split('?')[0];

							var qs = nextQuery.toString();
							let nextUrl = currentUrl;

							if (qs && qs.length) {
								nextUrl += '?' + qs;
							}

							// Todo: router needs the ability to change query string without
							// causing a refresh. This will primarily fix a weird jank that you'll experience 
							// if you edit the dropdowns from an ?sku= page 
							// (it will cause a page load and the set of dropdowns will probably all be empty) 
							setPage(nextUrl);
						}} />
					</div>
				</>}

				{/* price */}
				<ProductPrice product={currentVariant || product}
					override={hasVariants && !currentVariant ? cheapestPrice as CurrencyAmount : undefined}
					isFrom={!!hasVariants && !currentVariant} />

				{/* quantity / add to cart, only present if there is no variants or a variant is selected. */}
				{(!(product.variants?.length) || currentVariant) &&
					<ProductQuantity product={currentVariant || product} />}

				{props?.additionalActions?.map((component: string | React.FC<AdditionalActionProps>): React.FC<AdditionalActionProps> => {
					if (typeof component === 'string') {
						const Target = require(component)?.default;

						if (!Target) {
							throw new Error('Unable to find component ' + component);
						}

						return Target;
					}
					return component;
				}).map((Component) => <Component product={props.product} />)}
			</div>

			{/* suggested products */}
			{product.suggestions && product.suggestions.length > 0 && <>
				<div className="ui-product-view__suggestions">
					<h3 className="ui-product-view__suggestions-title">
						{`Similar Products`}
					</h3>
					<div className="ui-product-view__suggestions-list">
						<ProductList content={product.suggestions} viewStyle="small-thumbs" />
					</div>
				</div>
			</>}

		</div>
	</>;
}

export default View;