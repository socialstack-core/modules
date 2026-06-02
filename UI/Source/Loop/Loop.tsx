import { ApiList, ApiIncludes, ApiInclude } from 'UI/Functions/WebRequest';
import Failure from 'UI/Failed';
import Paginator from 'UI/Paginator';
import { AutoController, AutoControllerInt, ListFilter } from 'Api/Startup';
import { Content } from 'Api/Database';
import { ContentChangeDetail } from 'UI/Functions/ContentChange';
import useApi from 'UI/Functions/UseApi';
import { useEffect, useState } from 'react';
const DEFAULT_PAGE_SIZE = 50;

export type LoopStatus = "loading" | "empty" | "ready";

export interface LoopPageConfig {
	top?: boolean,

	bottom?:boolean,

	showInput?:boolean,
	noScroll?:boolean,

	maxLinks?:number,

	pageSize?: number,
}

export interface Filter<T> {
	where: Partial<Record<keyof (T), string | number | boolean>>
}

function mapWhere(where: any, args: any[]) {

	var str = '';
	if (Array.isArray(where)) {
		for (var i = 0; i < where.length; i++) {
			if (str) {
				str += where[i].and ? ' and ' : ' or ';
			}
			str += '(' + mapWhere(where[i], args) + ')';
		}
	} else {
		for (var k in where) {
			if (k == 'and' || k == 'op') {
				continue;
			}
			var v = where[k];
			if (v === undefined) {
				continue;
			}

			if (str != '') { str += ' and '; }

			if (Array.isArray(v)) {
				str += k + ' contains [?]'; // contains on an array is the same as containsAll. Different from containsAny and containsNone.
				args.push(v);
			} else if (v !== null && typeof v === 'object') {
				for (var f in v) {

					switch (f) {
						case "startsWith":
							str += k + " sw ?";
							args.push(v[f]);
							break;
						case "contains":
							str += k + " contains " + (Array.isArray(v[f]) ? '[?]' : '?');
							args.push(v[f]);
							break;
						case "containsNone":
						case "containsAny":
						case "containsAll":
							str += k + " " + f + " [?]";
							args.push(v[f]);
							break;
						case "endsWith":
							str += k + " endsWith ?";
							args.push(v[f]);
							break;
						case "geq":
						case "greaterThanOrEqual":
							str += k + ">=?";
							args.push(v[f]);
							break;
						case "greaterThan":
							str += k + ">?";
							args.push(v[f]);
							break;
						case "lessThan":
							str += k + "<?";
							args.push(v[f]);
							break;
						case "leq":
						case "lessThanOrEqual":
							str += k + "<=?";
							args.push(v[f]);
							break;
						case "not":
							str += k + "!=" + (Array.isArray(v[f]) ? '[?]' : '?');
							args.push(v[f]);
							break;

						case "name":
						case "equals":
							str += k + "=" + (Array.isArray(v[f]) ? '[?]' : '?');
							args.push(v[f]);
							break;
						default:
							break;
					}

				}
			} else {
				str += k + '=?';
				args.push(v);
			}
		}
	}

	return str;
}

/**
 * Converts where and on into a query formatted filter.
 * */
export function mapWhereToQuery(data: any): ListFilter | undefined {

	if (!data) {
		return undefined;
	}

	// Data exists - does it have a where style filter?
	if (data.where) {
		var where = data.where;
		var d2 = { ...data };
		delete d2.where;
		var args = [];
		var str = '';

		if (where.from && where.from.type && where.from.id) {
			str = 'From(' + where.from.type + ',?,' + where.from.map + ')';
			args.push(where.from.id);
			delete where.from;
		} else {
			str = '';
		}

		var q = mapWhere(where, args);

		if (q) {
			if (str) {
				// "From()" can only be combined with an and:
				str += ' and ' + q;
			} else {
				str = q;
			}
		}

		d2.query = str;
		d2.args = args;
		data = d2;
	}

	// this is done on list calls.
	if (data.on && data.on.type && data.on.id) {
		var on = data.on;
		var d2 = { ...data };
		delete d2.on;
		var onStatement = 'On(' + data.on.type + ',?' + (data.on.map ? ',"' + data.on.map + '"' : '') + ')';
		if (d2.query) {
			d2.query = '(' + d2.query + ') and ' + onStatement;
		} else {
			d2.query = onStatement;
		}
		if (!d2.args) {
			d2.args = [];
		}
		d2.args.push(data.on.id);
		data = d2;
	}

	return data || undefined;
}

/**
 * Base props for the Loop component.
 */
type LoopBaseProps<T> = {

	includes?: ApiInclude[];

	filter?: ListFilter;

	paged?: LoopPageConfig | boolean;

	/**
	 * set true to render paginator only (no overview)
	 */
	paginatorOnly?: boolean;

	/**
	 * set true to render overview only (no paginator)
	 */
	overviewOnly?: boolean;

	/** 
	 * set true to have paginator dock to bottom of parent
	 */
	dockBottom?: boolean;

	/**
	 * Custom failure handler.
	 * @param e
	 * @returns
	 */
	onFailure?: (e: PublicError) => React.ReactNode;

	/**
	 * Custom loader.
	 * @returns
	 */
	loader?: () => React.ReactNode;

	/**
	 * Custom message when no results are found.
	 * @returns
	 */
	orNone?: () => React.ReactNode;

	/**
	 * Child render function.
	 * @param item The item itself.
	 * @param index Current iteration index.
	 * @param fragmentCount The number of results in the current array. Not the same as the total number of results.
	 * @returns
	 */
	children: (item: T, index: number, fragmentCount: number) => React.ReactNode;

	/**
	 * Optional custom class name.
	 */
	className?: string

	/**
	 * Starting page index. The first page is assumed if not specified.
	 */
	defaultPage?: number,

	/**
	 * An optional function which can manipulate the results set before it is iterated.
	 * @param results
	 * @param listObj
	 * @returns
	 */
	onResults?: (results: T[], listObj: ApiList<T>) => T[]

	/**
	 * True to reverse the result set before iterating over it. 
	 * Note that this happens after onResults is invoked.
	 */
	reverse?: boolean,

	/**
	 * Is there a custom change handler, like URL based for instance.
	 * @param pageIndex
	 */
	customChangeHandler?: (pageIndex: number) => void;

	/**
	 * Optionally provide a layout function which handles any 
	 * necessary surrounding structure and the paginator placement (if there is one).
	 */
	onLayout?: (content: React.ReactNode, results: T[] | null, status: LoopStatus, paginator?: React.ReactNode, pageConfig?: LoopPageConfig) => React.ReactNode
}

// Define the version where 'over' is used
type LoopOverProps<T extends Content<uint>> = LoopBaseProps<T> & {
	over: AutoControllerInt<T>;
	source?: never; // Explicitly disallow source here
}

// Define the version where 'source' is used
type LoopSourceProps<T> = LoopBaseProps<T> & {
	over?: never; // Explicitly disallow over here
	source: (filter?: ListFilter, includes?: ApiInclude[]) => Promise<ApiList<T>>;
}

export type LoopProps<T> =
	T extends Content<uint>
	? LoopOverProps<T> | LoopSourceProps<T>
	: LoopSourceProps<T>;

/**
 * This component repeatedly renders its child using either an explicit array of data or an endpoint.
 */
const Loop = <T,>(props: LoopProps<T>) => {
	const { onLayout } = props;

	const [pageIndex, setPageIndex] = useState(props.filter?.pageIndex || props.defaultPage || 1);
	const [totalResults, setTotalResults] = useState(0);
	const [errored, setErrored] = useState<PublicError | null>(null);

	const filterStr = props.filter ? JSON.stringify(props.filter) : '';

	const load = (newPageIndex? : number) => {
		setErrored(null);

		if(newPageIndex !== undefined){
			setPageIndex(newPageIndex);
		}

		var pgIndex = newPageIndex === undefined ? pageIndex : newPageIndex;
		var filter = getPagedFilter(props.filter, pgIndex, props.paged);

		var source : Promise<ApiList<T>> | null = null;

		if (props.over) {
			var mappedFilter = mapWhereToQuery(filter);
			const overApi = props.over as AutoController<T, uint>;
			source = mappedFilter ? overApi.list(mappedFilter, props.includes) : overApi.listAll(props.includes);
		} else if (props.source) {
			const srcFunction = props.source as (filter?: ListFilter, includes?: ApiInclude[]) => Promise<ApiList<T>>;
			source = srcFunction(mapWhereToQuery(filter) as (ListFilter | undefined), props.includes);
		}

		if (!source) {
			return Promise.reject({
				type: 'loop/error',
				message: `No source`
			} as PublicError);
		}

		return source
		.then(list => {
			var results = list.results;

			if (props.onResults) {
				results = props.onResults(results, list) as T[];
			}

			if (props.reverse) {
				results = results.reverse();
			}

			setTotalResults(list.totalResults);
			return results;
		})
		.catch(e => {
			console.log('Loop caught an error:');
			console.error(e);

			if (e?.message) {
				setErrored(e as PublicError);
			} else {
				setErrored({
					type: 'loop/error',
					message: `An error occurred`,
					detail: e
				} as PublicError);
			}

			return null;
		});
	};

	const [results, setResults] = useApi<T[] | null>(() => {
		return load(props.filter?.pageIndex || props.defaultPage || 1);
	}, [filterStr, props.paged, props.over, props.source, props.includes]);

	useEffect(() => {
		var onContentUpdate = (e: CustomEvent<ContentChangeDetail>) => {
			const changeInfo = e.detail;
			const entity = changeInfo.entity as Content<uint>;

			if (!results || !entity) {
				return;
			}

			if (changeInfo.deleted) {
				var postDeleteResults = results
					.filter(content => {
						const ct = (content as Content<uint>);
						return !(ct.type == entity.type && ct.id == entity.id)
					});

				if (postDeleteResults.length != results.length) {
					setResults(postDeleteResults);
				}

			} else if (changeInfo.updated) {
				var changed = false;
				var updatedResults = results
					.map(content => {
						const ct = (content as Content<uint>);
						if (ct.type == entity.type && ct.id == entity.id) {
							changed = true;
							return changeInfo.entity as T;
						} else {
							return content;
						}
					});

				if (changed) {
					setResults(updatedResults);
				}
			} else if (changeInfo.added) {
				load().then(res => setResults(res));
			}
		};

		document.addEventListener("contentchange", onContentUpdate as EventListener);

		return () => {
			document.removeEventListener("contentchange", onContentUpdate as EventListener);
		};
	}, [results, load, setResults]);

	const getPagedFilter = (filter: any, pageIndex: number, paged?: LoopPageConfig | boolean) => {
		if (!paged) {
			return filter;
		}

		var pgCfg = getPageConfig(paged);

		if (!filter) {
			filter = {};
		}

		filter = { ...filter };
		filter.pageIndex = pageIndex - 1;
		filter.includeTotal = true;
		var pageSize = pgCfg.pageSize || DEFAULT_PAGE_SIZE;

		if (!filter.pageSize) {
			filter.pageSize = pageSize;
		}

		return filter;
	};
	
	if (errored) {
		// is a specific failure set?
		if (props.onFailure) {
			return props.onFailure(errored);
		}

		return <Failure />;
	}

	if (!results) {
		// Loading
		if (props.loader) {
			return props.loader();
		}

		return onLayout ? onLayout(null, null, "loading", undefined, undefined) : null;
	}

	var renderFunc = props.children;

	if (!results.length) {
		const emptyContent = props.orNone ? props.orNone() : null;
		return onLayout ? onLayout(emptyContent, results, "empty", undefined, undefined) : emptyContent;
	}

	var className = 'loop ';
	if (props.className) {
		className += props.className;
	}

	var content = results.map((item, i) => renderFunc(item, i, results.length));
	
	if(!props.paged){
		return onLayout ? onLayout(content, results, "ready", undefined, undefined) : content;
	}

	var pageCfg = getPageConfig(props.paged);
	var pageSize = pageCfg.pageSize || DEFAULT_PAGE_SIZE;
	var showInput = pageCfg.showInput !== undefined ? pageCfg.showInput : undefined;
	var maxLinks = pageCfg.maxLinks || undefined;
	var noScroll = pageCfg.noScroll || false;

	if(typeof pageCfg == "number"){
		pageSize = pageCfg;
	}
	
	// if filter contains pagesize use that
	if (props.filter?.pageSize) {
		pageSize = props.filter.pageSize;
	}

	var paginator = <Paginator
		pageSize={pageSize}
		showInput={showInput}
		maxLinks={maxLinks}
		pageIndex={props.filter?.pageIndex || pageIndex}
		totalResults={totalResults}
		paginatorOnly={props.paginatorOnly}
		overviewOnly={props.overviewOnly}
		dockBottom={props.dockBottom}
		onChange={(pageIndex: number) => {
			
			if (props.customChangeHandler) {
				props.customChangeHandler(pageIndex)
			}else {
				load(pageIndex).then(res => {
					setResults(res);
				});
				if (!noScroll) {
					window.scrollTo(0, 0);
				}
			}
		}}
		urlUpdating={Boolean(props.customChangeHandler)}
		key={filterStr}
	/>;

	if (onLayout) {
		return onLayout(content, results, "ready", paginator, pageCfg);
	}

	var result = [];
		
	if(pageCfg.top){
		result.push(paginator);
	}
	
	result.push(content);
	
	if(pageCfg.bottom !== false){
		// Bottom is true unless it's explicitly false
		result.push(paginator);
	}
	
	return result;
}

function getPageConfig(pgCfg: LoopPageConfig | true) {
	if (pgCfg === true) {
		return {} as LoopPageConfig;
	}

	return pgCfg as LoopPageConfig;
}

export default Loop;