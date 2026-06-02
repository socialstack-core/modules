import Link from 'UI/Link';
import Help from 'UI/Help';
import PromotionCycler, { InlinePromotion } from 'UI/PromotionCycler';

export type Crumb = {
	name: string,
	href: string
};

/**
 * Props for the Breadcrumb component.
 */
interface BreadcrumbProps {
	/**
	 * Show current page
	 */
	includeCurrent?: boolean,

	currentLabel?: string,

	crumbs?: Crumb[],

	help?: HtmlString,

	/**
	 * Promotions info
	 * (as of April 2026, only used by UI/Products/View)
	 */
	promotions?: InlinePromotion[],
	currentProductId?: int,
	currentProductPriceInPence?: int,
	currentCategoryId?: int,
	currentCategoryBreadcrumbs?: any[]
}

/**
 * The Breadcrumb React component.
 * @param props React props.
 */
const Breadcrumb: React.FC<BreadcrumbProps> = (props) => {
	const {
		includeCurrent, crumbs, currentLabel, help,
		promotions, currentProductId, currentProductPriceInPence,
		currentCategoryId, currentCategoryBreadcrumbs
	} = props;

	if (!crumbs) {
		return null;
	}

	return <nav className="site-breadcrumb">
		<menu>
			{
				crumbs.map(crumb => <li>
					<Link href={crumb.href} className="site-breadcrumb__item">
						<i className="fr fr-arrow-90"></i>
						<span>
							{crumb.name}
						</span>
					</Link>
				</li>)
			}
			{(includeCurrent || currentLabel) && <>
				<li className="site-breadcrumb__item--current-wrapper">
					<span className="site-breadcrumb__item site-breadcrumb__item--current">
						<i className="fr fr-chevron-right"></i>
						<span>
							{currentLabel || `Current page`}
						</span>
					</span>
				</li>
			</>}
		</menu>
		{!!help && <>
			<Help content={help} />
		</>}
		{promotions && promotions.length > 0 && <PromotionCycler promotions={promotions} currentProductId={currentProductId} currentProductPriceInPence={currentProductPriceInPence} currentCategoryId={currentCategoryId} currentCategoryBreadcrumbs={currentCategoryBreadcrumbs} />}
	</nav>;
}

export default Breadcrumb;