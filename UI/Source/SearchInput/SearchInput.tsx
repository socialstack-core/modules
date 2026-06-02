import { useEffect, useRef ,useState} from "react";
import { useRouter } from "UI/Router";
import Debounce from "UI/Functions/Debounce";
import Link from "UI/Link";
import {addToRecentSearches} from "UI/RecentSearches/SearchHistory";

/**
 * Props for the SearchInput component.
 */
interface SearchInputProps {
	/** 
	 * optional placeholder text for search field
	 */
	searchPlaceholder?: string,

	className?: string,

	/**
	 * Name of the URL query field to get/ set the input to. 
	 * Read from the query state to get the value of this bar.
	 */
	queryField?: string

	// Callback for when the query changes
	onQueryChange: (query : string) => void | Promise<void>;

	// Config to update query flag for search pages and history etc 
	updateQueryString?: boolean;

	// If user hits enter should they jump to the search page?
	linktoSearchPage?: boolean;

	// A label at the start of the bar if one is necessary.
	contextLabel?: string;
}

/**
 * Used for a searchbar which optionally updates the query string. 
 * Unlike UI/Search this is just the input itself - it does not perform any requests.
 * @param props React props.
 */
const SearchInput: React.FC<SearchInputProps> = (props:SearchInputProps) => {
	const { 
		searchPlaceholder,
		onQueryChange,
		contextLabel,
		updateQueryString = true,
		linktoSearchPage = false
	} = props;
	
    const { pageState, updateQuery, setPage } = useRouter();
	const queryField = props.queryField || 'q';
	const fromQueryString = pageState.query?.get(queryField) || '';
	const [qry, setQ] = useState(fromQueryString);

	const updateQueryRef = useRef(updateQuery);
	const lastCommittedQuery = useRef(fromQueryString);
	const debounce = useRef(
		new Debounce(
			(query: string) => {
				lastCommittedQuery.current = query;

				if (updateQueryString) {
					updateQueryRef.current({ [queryField]: query , page: undefined});
				}

				onQueryChange?.(query);
			}
		)
	);

	updateQueryRef.current = updateQuery;

	let searchInputClasses = ['ui-search-input'];

	if (contextLabel?.length) {
		searchInputClasses.push('input-group');
	}

	let searchClasses = ["form-control ui-form-control"];

	if (props.className) {
		searchClasses.push(props.className);
	}

	useEffect(() => {
		// Only update local state if the URL changed to something 
		// different than our last debounced execution.
		if (fromQueryString !== qry && fromQueryString !== lastCommittedQuery.current) {
			setQ(fromQueryString || '');
			onQueryChange?.(fromQueryString!);
			lastCommittedQuery.current = fromQueryString || '';
		}
	}, [fromQueryString]);

	return <div className={searchInputClasses.join(' ')}>
		{/* text-only version */}
		{/* contextLabel ? <span className="input-group-text">{contextLabel}</span> : null*/}
		{!!contextLabel && <>
			<Link href="/product/search" variant="info" xs className="ui-input-group-btn">
				<span>
					{contextLabel}
				</span>
				<i className="fr fr-times-alt"></i>
			</Link>
		</>}
		<input 
			type="search" 
			className={searchClasses.join(' ')} 
			placeholder={searchPlaceholder} 
			value={qry} 
			onInput={(ev) => {
				var qs = (ev.target as HTMLInputElement).value;
				setQ(qs);
				debounce.current.handle(qs);
			}} 
			onKeyDown={(ev) => {
				if (ev.key === "Enter" && linktoSearchPage) {
					setPage('/product/search/?q=' + encodeURIComponent((ev.target as HTMLInputElement).value));
				}
				if (ev.key === 'Enter') {
					// regardless of whether its on the search page or not.
					addToRecentSearches((ev.target as HTMLInputElement).value);
				}
			}}
		/>
	</div>;
}

export default SearchInput;