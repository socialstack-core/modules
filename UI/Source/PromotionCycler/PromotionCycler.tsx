import Promotion, {InlinePromotion, InlinePromotionConditions} from 'UI/Promotion';
import { ProductCategory } from 'Api/ProductCategory';
import { useState, useEffect } from 'react';

export {InlinePromotion};

interface Props {
	promotions?: InlinePromotion[];
	currentProductId?: int;
	currentProductPriceInPence?: int;
	currentCategoryId?: int;
	currentSearchCategoryId?: int;
	currentCategoryBreadcrumbs?: ProductCategory[];
}

const isValidCondition = (
	conditions: InlinePromotionConditions | undefined,
	placementType: string | undefined,
	currentProductId?: int,
	currentProductPriceInPence?: int,
	currentCategoryId?: int,
	currentSearchCategoryId?: int,
	currentCategoryBreadcrumbs?: ProductCategory[]
): boolean => {
	if (!conditions) return true;

	const now = new Date();

	if (conditions.pages && conditions.pages.length > 0) {
		return false;
	}

	if (conditions.searchCategories && conditions.searchCategories.length > 0) {
		const hasExactMatch = currentSearchCategoryId && conditions.searchCategories.includes(currentSearchCategoryId);
		
		if (!hasExactMatch) {
			// If includeChildren is true, also check parent categories
			if (conditions.includeChildren && currentCategoryBreadcrumbs?.length) {
				const parentIds = currentCategoryBreadcrumbs.map(c => c.id as int);
				const hasParentMatch = conditions.searchCategories.some(catId => parentIds.includes(catId));
				if (!hasParentMatch) {
					return false;
				}
			} else {
				return false;
			}
		}
	}

	if (conditions.categories && conditions.categories.length > 0) {
		const hasExactMatch = currentCategoryId && conditions.categories.includes(currentCategoryId);
		
		if (!hasExactMatch) {
			// If includeChildren is true, also check parent categories
			if (conditions.includeChildren && currentCategoryBreadcrumbs?.length) {
				const parentIds = currentCategoryBreadcrumbs.map(c => c.id as int);
				const hasParentMatch = conditions.categories.some(catId => parentIds.includes(catId));
				if (!hasParentMatch) {
					return false;
				}
			} else {
				return false;
			}
		}
	}

	if (conditions.products && conditions.products.length > 0) {
		if (!currentProductId || !conditions.products.includes(currentProductId as any)) {
			return false;
		}
	}

	// Price range is only checked for "product" placement type
	if (placementType === 'product' && (conditions.minPrice != null || conditions.maxPrice != null) && currentProductPriceInPence != null) {
		if (conditions.minPrice != null && currentProductPriceInPence < conditions.minPrice) {
			return false;
		}
		if (conditions.maxPrice != null && currentProductPriceInPence > conditions.maxPrice) {
			return false;
		}
	}

	return true;
};

const PromotionCycler = (props: Props) => {
	const [selectedPromo, setSelectedPromo] = useState<InlinePromotion | null>(null);

	useEffect(() => {
		if (!props.promotions || props.promotions.length === 0) return;
		
		const now = new Date();
		const validPromos = props.promotions.filter(p => 
			p.isActive && 
			(!p.startDate || new Date(p.startDate) <= now) && 
			(!p.endDate || new Date(p.endDate) >= now) &&
			isValidCondition(p.conditions, p.placementType, props.currentProductId, props.currentProductPriceInPence, props.currentCategoryId, props.currentSearchCategoryId, props.currentCategoryBreadcrumbs)
		);
		
		if (validPromos.length === 0) return;
		
		const randomIndex = Math.floor(Math.random() * validPromos.length);
		setSelectedPromo(validPromos[randomIndex]);
	}, [props.promotions, props.currentProductId, props.currentProductPriceInPence, props.currentCategoryId, props.currentSearchCategoryId, props.currentCategoryBreadcrumbs]);

	if (!selectedPromo) return null;
	
	return <Promotion promotion={selectedPromo} />;
};

export default PromotionCycler;
