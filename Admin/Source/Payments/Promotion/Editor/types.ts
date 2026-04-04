export type PlacementType = 'header' | 'search' | 'category' | 'product' | 'dashboard';

export type PlacementConditions = {
    pages?: (number | object)[];
    searchCategories?: (number | object)[];
    categories?: (number | object)[];
    includeChildren?: boolean;
    products?: (number | object)[];
    minPrice?: number | null;
    maxPrice?: number | null;
};

export type Placement = {
    type: PlacementType;
    conditions: PlacementConditions;
};

export const PlacementTypeLabels: Record<PlacementType, string> = {
    header: `Site Header`,
    search: `Product Search`,
    category: `Category Page`,
    product: `On Products`,
    dashboard: `User Dashboard`
};

export const PlacementTypeDescriptions: Record<PlacementType, string> = {
    header: `Appears sitewide in the header`,
    search: `Appears on product search results`,
    category: `Appears on category landing pages`,
    product: `Appears on specific products`,
    dashboard: `Appears on user dashboard`
};
