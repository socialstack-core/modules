import { useEffect, useRef } from "react";
import store from 'UI/Functions/Store';

export type SearchCriteria = {
	q?: string;
	page?: number;
    limit?:number;
	min?: number;
	max?: number;
	sort?: string;
	view?: string;
	inStockOnly?: boolean;
    approvalStatus?:string;
	hiddenProducts?:boolean;
    
	facets?: Record<number, number[]>; // attrId -> valueIds
	categoryId?:number;
	resetPrices?: boolean;
};

type UseCriteriaOpts = {
	pageState: { url: string; query: URLSearchParams };
	updateQuery: (update: Record<string, string | number | boolean | (string | number | boolean)[] | null | undefined>) => void;
	removeQueryItems: (keys: string[]) => void;
	defaults?: Partial<SearchCriteria>;
	onChange: (criteria: SearchCriteria) => void | Promise<void>;
	resetOnQChange?: Partial<SearchCriteria>;
	skipResetOnFirstLoad?: boolean;
};

const isEmpty = (v: unknown) =>
	v === undefined ||
	v === null ||
	(typeof v === "string" && v.trim() === "") ||
	(Array.isArray(v) && v.length === 0);

export function useRouterCriteria({
	pageState,
	updateQuery,
	removeQueryItems,
	defaults,
	onChange,
	resetOnQChange = {
		page: 1,
		min: undefined,
		max: undefined,
		facets: undefined,
		sort: undefined,
		inStockOnly: undefined,
		approvalStatus: undefined,
		hiddenProducts: undefined
	},
	skipResetOnFirstLoad = true
}: UseCriteriaOpts) {
	const signature = `${pageState.url}|${pageState.query.toString()}`;

	// Build single-value params + collect repeated facet keys
	const queryObj: Record<string, any> = {};
	const facetsMap: Record<number, number[]> = {};

	for (const [key, value] of pageState.query) {
		// extract facets -> facet[arttributeid]=id
		const m = key.match(/^facets\[(\d+)\]$/);
		if (m) {
			const attrId = Number(m[1]);
			const val = Number(value);
			if (!Number.isNaN(attrId) && !Number.isNaN(val)) {
				if (!facetsMap[attrId]) {
					facetsMap[attrId] = [];
				}
				facetsMap[attrId].push(val);
			}
			continue;
		}
		queryObj[key] = value;
	}

	const criteria = { ...(defaults ?? {}), ...queryObj } as SearchCriteria;
	if (Object.keys(facetsMap).length > 0) {
		criteria.facets = facetsMap;
	}

	// Coercions
	if (typeof criteria.page === "string" && criteria.page !== "") {
		criteria.page = Number(criteria.page);
	}
	if (typeof criteria.limit === "string" && criteria.limit !== "") {
		criteria.limit = Number(criteria.limit);
	}
	if (typeof criteria.min === "string" && criteria.min !== "") {
		criteria.min = Number(criteria.min);
	}
	if (typeof criteria.max === "string" && criteria.max !== "") {
		criteria.max = Number(criteria.max);
	}

	const toBool = (str?: string | boolean) => {
		if (typeof str === 'string') {
			return str === "true";
		}

		return str;
	};

	criteria.inStockOnly = toBool(criteria.inStockOnly);
	criteria.hiddenProducts = toBool(criteria.hiddenProducts);

	const viewType = criteria.view ?? store.get('productViewType') ?? 'small-thumbs';
	criteria.view = viewType;

	const sortType = criteria.sort ?? store.get('productSortType') ?? undefined;
	criteria.sort = sortType;

	// Notify on any URL change (including first load)
	const onChangeRef = useRef(onChange);
	onChangeRef.current = onChange;

	useEffect(() => {
		void onChangeRef.current(criteria);
	}, [signature]);

	// Auto-reset when q changes (skip on first load so that historic urls are retained)
	const hasInitializedRef = useRef<boolean>(false);
	const prevQRef = useRef<string | undefined>(undefined);

	useEffect(() => {
		const prevQ = prevQRef.current;
		const nextQ = criteria.q;

		// flag passed to force update of price range
		criteria.resetPrices = true;

		if (!hasInitializedRef.current) {
			hasInitializedRef.current = true;
			prevQRef.current = nextQ;
			if (skipResetOnFirstLoad) {
				return;
			}
		}

		// if query changes reset filters
		if (prevQ !== nextQ) {
			prevQRef.current = nextQ;

			const patch: Partial<SearchCriteria> = {};
			let changed = false;

			// get list of elments to reset from props
			for (const [k, v] of Object.entries(resetOnQChange)) {
				const key = k as keyof SearchCriteria;
				const target = v as any;
				const current = (criteria as any)[key];

				const shouldRemove =
					target === undefined ||
					target === null ||
					(typeof target === "string" && target.trim() === "") ||
					(Array.isArray(target) && target.length === 0);

				const isCurrentlyEmpty =
					current === undefined ||
					current === null ||
					(typeof current === "string" && current.trim() === "") ||
					(Array.isArray(current) && current.length === 0);

				if (shouldRemove) {
					if (!isCurrentlyEmpty) {
						patch[key] = undefined;
						changed = true;
					}
				} else {
					if (current !== target) {
						patch[key] = target;
						changed = true;
					}
				}
			}

			if (changed) {
				setCriteria(patch);
			}
		}
	}, [criteria.q]);

	function setCriteria(patch: Partial<SearchCriteria>) {
		// Special handling for facets (repeated keys)
		if (patch.facets !== undefined) {
			setFacets(patch.facets || {});
			// Continue with the rest of the criteria (without facets)
			const { facets, ...rest } = patch;
			applyPatch(rest);
			return;
		}

		applyPatch(patch);
	}

	function applyPatch(patch: Partial<SearchCriteria>) {
		const keysToRemove: string[] = [];
		const toUpdate: Record<string, string | number | boolean | (string | number | boolean)[]> = {};

		for (const [k, v] of Object.entries(patch)) {
			if (isEmpty(v)) {
				keysToRemove.push(k);
			} else {
				toUpdate[k] = v as any;
			}
		}

		if (keysToRemove.length) {
			removeQueryItems(keysToRemove);
		}
		if (Object.keys(toUpdate).length) {
			updateQuery(toUpdate);
		}
	}

	function setFacets(next: Record<number, number[]>) {
		// Build a single update payload
		const update: Record<
			string,
			string | number | boolean | (string | number | boolean)[] | null | undefined
		> = {};

		// Mark *all existing facets* for deletion
		for (const [k] of pageState.query) {
			if (/^facets\[\d+\]$/.test(k)) {
				update[k] = ""; // falsy → delete
			}
		}

		// Add the new facets in one go
		for (const [attrIdStr, values] of Object.entries(next)) {
			const attrId = Number(attrIdStr);
			if (!Number.isNaN(attrId) && Array.isArray(values) && values.length > 0) {
				update[`facets[${attrId}]`] = values.map((n) => Number(n));
			}
		}

		// One atomic write, no intermediate empty state
		updateQuery(update);
	}

	function removeCriteria(keys: (keyof SearchCriteria)[]) {
		// Removing "facets" clears all facet keys
		if (keys.includes("facets")) {
			const allFacetKeys: string[] = [];
			for (const [k] of pageState.query) {
				if (/^facets\[\d+\]$/.test(k)) {
					allFacetKeys.push(k);
				}
			}
			if (allFacetKeys.length) {
				removeQueryItems(allFacetKeys);
			}
			return;
		}
		removeQueryItems(keys.map(String));
	}

	function clearAllCriteria(excluded: (keyof SearchCriteria)[] = []) {
		const allKeys: string[] = [];
		for (const [k] of pageState.query) {

			if (excluded.includes(k as keyof SearchCriteria)) {
				continue;
			}

			allKeys.push(k);
		}
		if (allKeys.length) {
			removeQueryItems(allKeys);
		}
	}

	function hasCriteria(): boolean {
		const keys = Object.keys(criteria) as (keyof SearchCriteria)[];
		
		// Check if there's any key other than resetPrices
		return keys.some(key => key !== "resetPrices");
	}

	return { criteria, hasCriteria, setCriteria, removeCriteria, clearAllCriteria };
}
