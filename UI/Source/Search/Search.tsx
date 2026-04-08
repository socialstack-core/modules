import { useState, useEffect, useRef } from 'react';
import { ApiIncludes } from 'Api/Includes';
import { ListFilter, Content } from 'Api/Content'
import { ApiList } from 'UI/Functions/WebRequest';

export type SearchProps<T extends Content<uint>> = {
    startHidden?: boolean;
    value?: string;
    minLength?: number;
    exclude?: uint[];
    includes?: ApiIncludes;
    field?: string;
    limit?: number;
    onResults?: (results: T[]) => void;
    onQuery?: (filter: ListFilter, query: string) => void;
    onFind?: (result: T) => void;
    // this should render some DOM when called
    // the usage in the component is an LTR op. 
    onRender?: (result: T) => React.ReactElement | void;
    placeholder?: string;
    searchText?: string;
    name?: string;
    className?: string;
    inputClassName?: string;
    'data-theme'?: string;
    endpoint?: (filter?: ListFilter, includes?: ApiIncludes) => Promise<ApiList<T>>;
    onInput?: (value: string) => void,
	onNoResults?: () => React.ReactNode
};

type NoFieldWhereQuery = {
    name: string
}

/**
 * Used to search for things.
 */
const Search = <T extends Content<uint>,>(props: SearchProps<T>) => {

    const { onFind, exclude, name } = props;
	
	let { onNoResults } = props;
	
	if (!onNoResults) {
		onNoResults = () => <div className="no-results">No results found</div>;
	}
	
    const [loading, setLoading] = useState<boolean>(false);
    const [hidden, setHidden] = useState<boolean>(Boolean(props.startHidden));
    const [results, setResults] = useState<T[] | null>(null); // Typed results as T[]
    const [selected, setSelected] = useState<T | null>(null);
    const [dropUp, setDropUp] = useState<boolean>(false);

    const inputRef = useRef<HTMLInputElement | null>(null);
    const suggestionsRef = useRef<HTMLDivElement | null>(null);

    // render the search result expanding any fields as neccessary
    const renderResult = props.onRender || ((result: any) => {
        const key = props.field ? props.field[0].toLowerCase() + props.field.substring(1) : 'name';
        return result[key];
    });
	
	const { field } = props;


	// Function to fetch search results from endpoint
    const fetchResults = (query: string) => {
        
        // empty strings are falsy, 
        // when its empty nothing should happen.
        // when no minLength is passed
        if (!query) {
            setResults(null);
			props?.onQuery?.({
				query: field + ' contains ?',
				args: [query]
			}, query)
            return;
        }
        
        if (props.minLength && query.length < props.minLength) {
            setResults(null); // Clear results if query is too short
            return;
        }

        setLoading(true); // Set loading to true while fetching
        
        var filter : ListFilter = {
            query: field + " contains ?",
            args: [query]
        };

        const { includes } = props;

        props.onQuery && props.onQuery(filter, query)

        props.endpoint && props.endpoint(filter, includes)
            .then((fetchedResults) => {

                var res = fetchedResults.results;

                if (exclude) {
                    res = res.filter(r => !exclude.find(id => id == r.id));
                }

                setResults(res);
                setLoading(false); // Set loading to false after fetching
            })
            .catch((error) => {
                console.error('Error fetching results:', error);
                setLoading(false); // Set loading to false on error
            });
    };

    useEffect(() => {
        if (results) {
            props.onResults && props.onResults(results);
        }
    }, [results, props])

    useEffect(() => {
        if (results && !props.onResults && inputRef.current && suggestionsRef.current) {
            const inputRect = inputRef.current.getBoundingClientRect();
            const suggestionsHeight = suggestionsRef.current.offsetHeight;
            const spaceBelow = window.innerHeight - inputRect.bottom;
            const spaceAbove = inputRect.top;

            if (spaceBelow < (suggestionsHeight + 10) && spaceAbove > spaceBelow) {
                setDropUp(true);
            } else {
                setDropUp(false);
            }
        } else if (!results) {
            setDropUp(false);
        }
    }, [results, props.onResults]);

    const selectValue = (value: T) => {
        onFind && onFind(value);
        setSelected(value);
    };
	
    return (
		<div className={`search ${props.className}`} data-theme={props['data-theme'] || 'search-theme'}>
            <input
				ref={inputRef}
				name={name}
                onBlur={() => setResults(null)} // Clear results on blur
                autoComplete="false"
                className={`form-control ui-form-control ${props.inputClassName || ''}`}
                defaultValue={props.searchText}
                placeholder={props.placeholder || 'Search...'}
                type="text"
                onInput={(e) => {
                    props.onInput && props.onInput((e.target as HTMLInputElement).value)
                }}
                onKeyUp={(e) => {
                    fetchResults((e.target as HTMLInputElement).value); 
                }}
                onFocus={(e) => {
                    if (e.target.value.length > 0) {
                        fetchResults((e.target as HTMLInputElement).value); // Trigger fetch on focus if text is present
                    }
                }}
                onKeyDown={(e) => {
                    if (e.key === 'Enter' && results && results.length === 1) {
                        selectValue(results[0]); // Select the first result if it's a match
                    }
					if (e.key === 'Enter') {
						e.preventDefault();
						e.stopPropagation();
					}
                    if (e.key === 'Escape') {
                        setResults(null); // Clear results on escape
                    }
                }}
            />
            {results && !props.onResults && (
                <div className={`suggestions ${dropUp ? 'suggestions-up' : ''}`} ref={suggestionsRef}>
                    {results.length ? (
                        results.map((result, i) => (
                            <button
                                type="button"
                                key={i}
                                onMouseDown={() => selectValue(result)}
                                className="btn suggestion"
                            >
                                {/* Customize the display of the result here */}
                                {result && renderResult(result as any)}

                            </button>
                        ))
                    ) : (
                        onNoResults()
                    )}
                </div>
            )}
            {props.name && <input type="hidden" value={(selected ? selected.id : '') || props.value} name={props.name} />}
        </div>
    );
};

export default Search;
