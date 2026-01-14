import { Product } from "Api/Product";
import { useEffect, useState } from "react";
import store from 'UI/Functions/Store'
import Signpost from "UI/Product/Signpost";

/**
 * React component that displays a list of recently searched products in a horizontal scrollable list.
 *
 * The recent searches are intended to provide quick access to previously viewed or queried products.
 * Currently, the data is statically generated using `generateDummyData()` and rendered using the
 * `RecentSearchProductItem` component.
 *
 * ### Features:
 * - Renders a scrollable horizontal list of product items.
 * - Displays product name and image using the child component.
 * - Stubbed data generation to be replaced by actual search history logic.
 *
 * ### Future Enhancements:
 * - Replace `generateDummyData()` with actual user search history from a backend or localStorage.
 * - Add click tracking or navigation logic when items are interacted with.
 * - Limit number of stored searches and add deduplication logic.
 *
 * @component
 * @example
 * ```tsx
 * <RecentSearches />
 * ```
 *
 * @returns {React.ReactElement} The rendered list of recent product searches.
 */
const RecentSearches: React.FC<{}> = (props): React.ReactElement | null => {
    /**
     * State variable holding the list of recently searched products.
     * This will be populated with real user history in a later iteration.
     */
    const [recentSearches, setRecentSearches] = useState<Product[]>([]);

    /**
     * Temporary effect to populate the `recentSearches` state with mock data.
     * This should be removed or replaced when real search data is available.
     */
    useEffect(() => {
        setRecentSearches(store.get('recent_searches') ?? []);
    }, [recentSearches]);

    return (
		recentSearches?.length ? <div className={'recent-searches'}>
            <div className={'panel-header'}>
                {`Recent searches`}
            </div>
            <div className={'panel-body'}>
                <ul className={'recent-searches-product-list'}>
                    {recentSearches.map(product => (
                        <Signpost content={product} viewStyle={'small-thumbs'}/>
                    ))}
                </ul>
            </div>
        </div> : null
    );
};

export default RecentSearches;
