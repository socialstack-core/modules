import webRequest from 'UI/Functions/WebRequest';
import Column from 'UI/Column';
import Row from 'UI/Row';
import Content from 'UI/Content';
import webSocket from 'UI/Functions/WebSocket';
import getEndpointType from 'UI/Functions/GetEndpointType';
import Failure from 'UI/Failed';
import Paginator from 'UI/Paginator';
import { isoConvert } from 'UI/Functions/DateTools';
const DEFAULT_PAGE_SIZE = 50;

// Operators as used by filter in where clauses.
const filterOperators = {
	"startsWith": (a, b) => a && a.indexOf(b) == 0,
	"contains": (a, b) => a && a.indexOf(b) != -1,
	"endsWith": (a, b) => a && a.substring(a.length - b.length, a.length) === b,
	"geq": (a, b) => a >= b,
	"greaterThanOrEqual": (a, b) => a >= b,
	"greaterThan": (a, b) => a > b,
	"lessThan": (a, b) => a < b,
	"leq": (a, b) => a <= b,
	"lessThanOrEqual": (a, b) => a <= b,
	"not": (a, b) => a != b,
	"equals": (a, b) => a == b
};

/**
 * This component repeatedly renders its child using either an explicit array of data or an endpoint.
 */
export default class Loop extends React.Component {
	
	constructor(props) {
		super(props);
		
		// Initial state which may come from the first render cache.
		if(Array.isArray(props.over)){
			
			// Load initial result set now:
			this.state = this.loadDirectArray(props, 1);
			
		}else if(typeof props.over === 'string'){
			
			var firstSlash = props.over.indexOf('/');
			var cached;
			if(firstSlash == -1 || props.over.substring(firstSlash+1) == 'list'){
				cached = Content.listCached(firstSlash == -1 ? props.over : props.over.substring(0, firstSlash), props.filter, props.includes);
			}
			
			if(cached){
				if(cached.then){
					// Special case on the server. It's a promise, but the result needs to be processed immediately.
					// Only this constructor executes if a promise is in the state, so it's ok for it to directly overwrite all of this.state when its done.
					cached = cached.then(r => {
						this.state = this.processCached(r, props);
					});
					this.state = {
						cached
					};
				}else{
					this.state = this.processCached(cached, props);
				}
			}else{
				// Can only client side load this one
				this.state = {
					pageIndex: 1
				};
			}
		}else{
			// Invalid!
		}

		this.onLiveMessage = this.onLiveMessage.bind(this);
		this.onContentChange = this.onContentChange.bind(this);
	}
	
	processCached(pending, props){
		var results = (pending && pending.results) ? pending.results : [];
		
		if (props.onResults) {
			results = props.onResults(results);
		}

		if (props.reverse) {
			results = results.reverse();
		}
		
		return {
			results, errored: false, totalResults: pending.total
		};
	}
	
	componentWillMount(){
		// Required for the server, as the _pending object above is a promise which 
		var {_pending} = this.state;
		var props = this.props;
		
		if(_pending){
			
		}
	}
	
	onLiveMessage(msg) {
		if (msg.all) {
			if (msg.type == "status") {
				if (msg.connected) {
					// Force a reload:
					this.load(this.props);
				}

				this.props.onLiveStatus && this.props.onLiveStatus(msg.connected);
			}
			return;
		}

		// Push msg.entity into the results set:
		if (this.state.results && msg.entity) {
			var e = msg.entity;
			
			var entityId = e.id;

			if (msg.method == 'delete') {
				// Remove by id:
				this.setState({ results: this.state.results.filter(ent => ent && ent.id != entityId) });
			} else if (msg.method == 'update') {
				// find by id:

				var res = this.state.results;
				var found = false;
				for (var i = 0; i < res.length; i++) {
					if (!res[i]) {
						continue;
					}

					if (res[i].id == entityId) {
						res[i] = e;
						found = true;
					}
				}

				if (found) {
					// If it still passes the filter, keep it. Otherwise, delete it.
					var keep = this.testFilter(e);

					if (keep && !this.isSorted()) {
						this.setState({ results: res });
					} else {
						// Delete it:
						res = res.filter(ent => ent && ent.id != entityId);

						if (keep) {
							// Re-add it (into a sorted list). This allows an updated entity to move change order.
							this.add(e, res, msg);
						} else {
							this.setState({ results: res });
						}
					}
				} else {

					// Does it pass the filter? If it does, add it.
					if (this.testFilter(e)) {
						this.add(e, res, msg);
					}

				}

			} else if (msg.method == 'create') {

				// already in there?
				var results = this.state.results;

				if (!results.find(ent => ent && ent.id == entityId)) {

					// Nope - potentially adding it.
					// First though, make sure it passes the filter if there is one.
					if (this.testFilter(msg.entity)) {
						this.add(msg.entity, results, msg);
					}
				}

			}

		}
	}

	// Finds the index where the given item should be inserted.
	sortedSearch(arr, item, compare, low, high) {
		if (high <= low) {
			return compare(item, arr[low]) == 1 ? (low + 1) : low;
		}

		// Mid coerced to int:
		var mid = ((low + high) / 2) | 0;

		if (compare(item, arr[mid]) == 0) {
			return mid + 1;
		}

		if (compare(item, arr[mid]) == 1) {
			return this.sortedSearch(arr, item, compare, mid + 1, high);
		}
		return this.sortedSearch(arr, item, compare, low, mid - 1);
	}

	isSorted() {
		var { filter } = this.props;
		return filter && filter.sort && filter.sort.field;
	}

	add(entity, results, msg) {

		var { filter, reverse } = this.props;

		if (results.length && this.isSorted()) {
			// account for the sort too.

			// Ascending is default.
			var desc = (filter.sort.dir || filter.sort.direction) == 'desc';

			if (reverse) {
				desc = !desc;
			}

			var positive = desc ? -1 : 1;
			var negative = -positive;

			// The sort field may have an uppercase first letter:
			var fieldName = filter.sort.field;
			fieldName = fieldName.charAt(0).toLowerCase() + fieldName.slice(1);

			// Read the field:
			var fieldValue = entity[fieldName];

			// Date field? Special case for those:
			var isDate = fieldName.endsWith('Utc');

			if (isDate) {
				// Load str:

				if(fieldValue) {
					fieldValue = isoConvert(fieldValue)
					if(fieldValue) {
						fieldValue = fieldValue.getTime();
					}
				} else {
					fieldValue = null;
				}
			}

			// Search for the target index:
			var targetIndex = this.sortedSearch(results, fieldValue, (a, b) => {
				// a is the field value from the entity.
				// b is the object to compare with.
				var bField = b ? b[fieldName] : null;

				if (isDate) {
					// Parse the date:
					if (typeof bField == 'string') {
						bField = isoConvert(bField);
						if(bField) {
							bField = bField.getTime();
						}

					} else if (bField && bField.getTime) {
						bField = bField.getTime();
					} else {
						bField = isoConvert(bField);
						if(bField) {
							bField = bField.getTime();
						}
					}
				}

				if (a > bField) {
					return positive;
				}

				return (a == bField) ? 0 : negative;
			}, 0, results.length - 1);

			// Push it in:
			results.splice(targetIndex, 0, entity);

		} else {
			results.push(entity);
		}

		this.props.onLiveCreate && this.props.onLiveCreate(entity, msg);
		this.setState({ results });
	}
	
	testFilter(ent) {
		var { filter } = this.props;

		if (filter && filter.where) {
			var w = filter.where;
			
			if(Array.isArray(w)){
				if(!w.length){
					return true;
				}
				for(var i=0;i<w.length;i++){
					if(this.testFilterObj(ent, w[i])){
						return true;
					}
				}
				return false;
			}else{
				if(!this.testFilterObj(ent, w)){
					return false;
				}
			}

		}

		return true;
	}

	testFilterObj(ent, where){
		for (var key in where) {
			if (!key || !key.length) {
				continue;
			}
			
			var value = where[key];
			
			if(value === undefined){
				continue;
			}
			
			// Lowercase the key as it's an exact field match:
			var entityKeyName = key.charAt(0).toLowerCase() + key.slice(1);
			
			// Date field? Special case for those:
			var isDate = entityKeyName.endsWith('Utc');
			
			var reqValue = ent[entityKeyName];
			
			if(isDate){
				reqValue = isoConvert(reqValue)
				if(reqValue){
					reqValue = reqValue.getTime()
				}
			}
			
			// value can be an array of options:
			if (Array.isArray(value)) {
				// Failed the filter if the reqd value is not in the array.
				return value.indexOf(reqValue) != -1;
			} else if (value && typeof value == 'object') {
				// Potentially defining an operator other than equals.
				// {not: 'test'} etc.
				var success = false;

				for (var filterField in filterOperators) {
					var fValue = value[filterField];

					// Basic check to make sure it's not a function.
					// - It can be null though
					
					if (fValue !== undefined && (fValue === null || isDate || typeof fValue != 'object')) {

						// Use this operator. Special case for dates.
						if(isDate){
							fValue = isoConvert(fValue)
							if(fValue){
								fValue = fValue.getTime();
							}
						}
						
						if (!filterOperators[filterField](reqValue, fValue)) {
							return false;
						} else {
							success = true;
							break;
						}
					}
				}

				if (success) {
					// Didn't get any matching filter operators otherwise so it should just attempt an object v. object compare
					continue;
				}
			}
			
			if(isDate){
				value = isoConvert(value);
				if(value) {
					value = value.getTime();
				}
			}
			
			if (reqValue != value) {
				return false;
			}
		}
		
		// All clear
		return true;
	}

	onContentChange(e) {

		// Content changed! Is it relevant to this loop?
		var results = this.state.results;

		if (typeof this.props.over != 'string' || !results) {
			// Not looping an endpoint OR we don't have any results yet anyway.
			return;
		}
		
		if (this.props.updateContentType) {
			// If you're using custom endpoints, specify this updateContentType prop to be able to still receive live updates.

			if (this.props.updateContentType != e.entity.type) {
				// This content isn't of the same type as this loop.
				return;
			}

		} else if (getEndpointType(this.props.over).type != e.endpointType) {
			// This content isn't of the same type as this loop.
			return;
		}

		/*
		Unified with liveMessage
		if (this.props.onContentChange) {

			entity = this.props.onContentChange(entity);

			if (!entity) {
				// Handler rejected
				return;
			}
		}
		*/
		
		var entity = e.entity;
		this.onLiveMessage({entity, method: e.deleted ? 'delete' : (e.created ? 'create' : 'update')});
	}

	componentDidMount() {
		// contentchange is fired off by posting to API endpoints which then return an entity (object with both id and type fields).
		document.addEventListener("contentchange", this.onContentChange);
		
		if(!this.state.results){
			// No cached results - load now:
			this.load(this.props);
		}
	}

	componentWillUnmount() {
		if (this.props.live) {
			var liveInfo = this.liveType(this.props);
			webSocket.removeEventListener(liveInfo.type, this.onLiveMessage);
		}

		document.removeEventListener("contentchange", this.onContentChange);
	}

	componentDidUpdate(prevProps) {
		var {over, filter} = this.props;
		
		// Has the filter/ over prop changed?
		if(
			over != prevProps.over || 
			(Array.isArray(over) && over.length != prevProps.over.length) || 
			JSON.stringify(filter) != JSON.stringify(prevProps.filter)
		){
			this.load(this.props, 1);
		}
	}
	
	getPagedFilter(filter, pageIndex, paged){
		if (paged) {
			if (!filter) {
				filter = {};
			}
			filter = { ...filter };
			filter.pageIndex = pageIndex - 1;
			filter.includeTotal = true;
			var pageSize = paged.pageSize || DEFAULT_PAGE_SIZE;

			if (typeof paged == "number") {
				pageSize = paged;
			}
			if (!filter.pageSize) {
				filter.pageSize = pageSize;
			}
		}
		return filter;
	}
	
	liveType(props){
		// Type name:
		var type = props.over.split('/')[0].toLowerCase();
		var id=0;
		var onFilter = null;
		
		// If the filter is the equiv of "Id=?", it's type with the provided ID:
		var filter = props.filter;
		if(filter){
			if(filter.where){
				if(!Array.isArray(filter.where)){
					var {where} = filter;
					var keySet = Object.keys(where);
					if(keySet.length == 1){
						var firstKey = keySet[0].toLowerCase();
						if(firstKey == 'id' && typeof where[keySet[0]] != 'object'){
							id = parseInt(where[keySet[0]]);
						}
					}
				}
			}else if(filter.on){
				var on = filter.on;
				onFilter = {query: on.map ? 'On(' + on.type + ',?,"' + on.map + '")' : 'On(' + on.type + ',?)', args: [parseInt(on.id)]};
			}
		}
		
		if(props.liveFilter){
			onFilter = props.liveFilter;
		}
		
		return {type, id, onFilter};
	}
	
	load(props, newPageIndex) {
		if (typeof props.over == 'string') {
			if (props.live) {
				// Note: onLiveMessage is used to detect if the filter changed
				var liveInfo = this.liveType(props);
				webSocket.addEventListener(liveInfo.type, this.onLiveMessage, liveInfo.id, liveInfo.onFilter);
			}
			
			var newState = {
				errored: false 
			};
			
			if(newPageIndex){
				newState.pageIndex = newPageIndex;
			}
			
			this.setState(newState);
			var filter = this.getPagedFilter(props.filter, newPageIndex || this.state.pageIndex, props.paged);
			
			// NB: still using webRequest here rather than Content.list because props.over can also be custom named endpoints (for now!)
			webRequest(props.over.indexOf('/') == -1 ? props.over + '/list' : props.over, filter, props.requestOpts ? {includes: props.includes, ...props.requestOpts} : {includes: props.includes}).then(responseJson => {
				var responseJson = responseJson.json;
				var results = (responseJson && responseJson.results) ? responseJson.results : [];

				if (props.onResults) {
					results = props.onResults(results);
				}

				if (props.reverse) {
					results = results.reverse();
				}

				this.setState({ results, errored: false, totalResults: responseJson.total });
			}).catch(e => {
				console.log('Loop caught an error:');
				console.error(e);

				var results = [];

				if (props.onResults) {
					results = this.props.onResults(results);
				}

				if (props.reverse) {
					results = results.reverse();
				}

				if (props.onFailed) {
					if (props.onFailed(e)) {
						this.setState({ results, totalResults: results.length, errored: false });
						return;
					}
				}

				this.setState({ results, totalResults: results.length, errored: true, errorMessage: e });
			});

		} else {
			this.setState(this.loadDirectArray(props, newPageIndex));
		}
	}
	
	// Returns state change
	loadDirectArray(props, newPageIndex) {
		var results = props.over;
		
		if (props.onResults) {
			results = props.onResults(results);
		}
		
		var pageCfg = props.paged;
		var totalResults = results.totalResults || results.length;
		
		if (pageCfg) {
			var offset = (newPageIndex || this.state.pageIndex)-1;
			var pageSize = pageCfg.pageSize || DEFAULT_PAGE_SIZE;
			
			if (typeof pageCfg == "number") {
				pageSize = pageCfg;
			}
			
			var startIndex = offset * pageSize;
			results = results.slice(startIndex, startIndex + pageSize);
		}

		if (props.reverse) {
			results = results.reverse();
		}
		
		var newState = { over: null, jsonFilter: null, results, totalResults, errored: false };
		
		if(newPageIndex){
			newState.pageIndex = newPageIndex;
		}
		
		return newState;
	}
	
	render() {
		
		if (this.state.errored) {
			// is a specific failure set?
			if (this.props.onFailure) {
				if (typeof this.props.onFailure === "function") {
					return this.props.onFailure(this.state.errorMessage);
				}
			}
			return <Failure />
		}

		if (!this.state.results && !this.props.items) {
			// Loading
			if (this.props.loader) {

				if (typeof this.props.loader === "function") {
					return this.props.loader();
				} else {
					return `Loading...`;
				}

			}

			return null;
		}

		var results = this.state.results;

		var renderFunc = this.props.children;

		if (this.props.items) {
			results = this.props.items;

			// Use the provided renderer instead:
			var Module = results.renderer;

			renderFunc = (item, i, count) => {
				return <Module item={item} container={this.props} />
			};
		}

		if (!results.length || this.props.testNone) {
			// No results at all.
			var M = this.props.noneDisplayer;
			if (M) {
				return <M loopAllProps={this.props} />;
			}

			return this.props.orNone ? this.props.orNone() : null;
		}

		var className = 'loop ' + (this.props.name ? this.props.name : ((typeof this.props.over == 'String') ? this.props.over.replace('/', '-') : ''));
		if (this.props.className) {
			var className = this.props.className;
		}

		if (this.props.groupAll) {
			// Call the child method just once with the results as a single block.
			results = [results];
		}

		if (this.props.inGroupsOf && this.props.inGroupsOf > 0) {
			// Group up results into blocks of x entries.
			var newResults = [];
			var groupsOf = this.props.inGroupsOf;
			for (var i = 0; i < results.length; i += groupsOf) {
				newResults.push(results.slice(i, i + groupsOf));
			}
			results = newResults;
		}

		var mode = this.props.mode;

		if (this.props.colXs || this.props.colSm || this.props.colMd || this.props.colLg || this.props.colXl) {
			mode = "col";
		} else if (this.props.altRow) {
			mode = "altrow";
		} else if (this.props.asTable) {
			mode = "table";
		} else if (this.props.asUl) {
			mode = "ul";
		} else if (this.props.asCols) {
			mode = "cols";
		} else if (this.props.raw) {
			mode = "raw";
		} else if (this.props.inline) {
			mode = "inline";
		}
		
		var loopContent = null;
		
		switch (mode) {
			case "inline":
				loopContent = (
					<span className={className}>
						{
							results.map((item, i) => {
								return (
									<span className={'loop-item loop-item-' + i} key={i}>
										{renderFunc(item, i, results.length)}
									</span>
								);
							})
						}
					</span>
				);
				break;
			case "raw":
				loopContent = results.map((item, i) => {
					return renderFunc(item, i, results.length);
				});
				break;
			case "unformatted":
				loopContent = (<span className={className}>
					{results.map((item, i) => {
						return renderFunc(item, i, results.length);
					})}
				</span>
				);
				break;
			case "cols":
			case "columns":
				var size = parseInt(this.props.size);

				if (!size || isNaN(size)) {
					size = 4;
				}

				if (size <= 0) {
					size = 1;
				} else if (size > 12) {
					size = 12;
				}

				var colCount = Math.floor(12 / size);
				var rowCount = Math.ceil(results.length / colCount);
				var col = 0;
				var rows = [];

				for (var r = 0; r < rowCount; r++) {
					var cols = [];

					for (var c = 0; c < colCount; c++) {
						if (col >= results.length) {
							break;
						}

						var item = results[col];

						cols.push(
							<Column size={size} className={'loop-item loop-item-' + col} key={c}>
								{renderFunc(item, i, results.length)}
							</Column>
						);
						col++;
					}

					rows.push(<Row className="loop-row" key={r}>{cols}</Row>);
				}

				loopContent = <div className={className}>{rows}</div>;
				break;
			case "ul":
			case "bulletpoints":
				loopContent = (
					<ul className={className} {...this.props.attributes}>
						{
							results.map((item, i) => {
								return (
									<li className={'loop-item loop-item-' + i + ' ' + (this.props.subClassName ? this.props.subClassName : '')} key={i}>
										{renderFunc(item, i, results.length)}
									</li>
								);
							})
						}
					</ul>
				);
				break;
			case "table":
				// May have multiple render functions, including for the header and footer.
				var headerFunc = null;
				var colgroupsFunc = null;
				var bodyFunc = null;
				var footerFunc = null;

				if (!this.props.items) {

					if (renderFunc instanceof Array) {

						renderFunc.forEach(func => {
							let funcResults = func({}, 0, 0);
							let funcType;

							if (funcResults instanceof Array && funcResults.length) {
								funcType = funcResults[0].type;
							} else if (typeof funcResults === 'object' && funcResults !== null) {
								if(funcResults.props && funcResults.props.children && funcResults.props.children.length) {
									funcType = funcResults.props.children[0].type;
								} else {
									funcType = funcResults.type;
								}
							}

							switch (funcType) {
								case 'th':
									headerFunc = func;
									break;

								case 'col':
									colgroupsFunc = func;
									break;

								case 'td':

									if (!bodyFunc) {
										bodyFunc = func;
									} else {
										footerFunc = func;
									}

									break;
							}

						});

					} else {
						bodyFunc = renderFunc;
					}

				}

				loopContent = (
					<table className={"table " + className + (this.props.captionTop ? ' caption-top' : '')}>
						{headerFunc && (
							<thead>
								<tr>
								{
									headerFunc(results)
								}
								</tr>
							</thead>
						)}
						{colgroupsFunc && (
							<colgroup>
								{
									colgroupsFunc(results)
								}
							</colgroup>
						)}
						{bodyFunc && <>
							<tbody>
								{
									results.map((item, i) => {
										return (
											<tr className={'loop-item loop-item-' + i} key={i}>
												{bodyFunc(item, i, results.length)}
											</tr>
										);
									})
								}
							</tbody>
						</>}
						{footerFunc && (
							<tfoot>
								{
									footerFunc(results)
								}
							</tfoot>
						)}
						{this.props.caption && (
							<caption>
								{this.props.caption}
							</caption>
						)}
					</table>
				);
				break;
			case "col":
				var breakpoints = ['Xs', 'Sm', 'Md', 'Lg', 'Xl'];
				var breakpointClasses = "";

				breakpoints.forEach((breakpoint) => {
					var width = this.props["col" + breakpoint];

					if (width > 0) {
						breakpointClasses += " col-" + breakpoint.toLowerCase() + "-" + width;
					}
				});
				
				loopContent = (
					<div className={className}>
						{
							results.map((item, i) => {
								var classes = breakpointClasses + ' loop-item loop-item-' + i;
								return (
									<div className={classes} key={i}>
										{renderFunc(item, i, results.length)}
									</div>
								);
							})
						}
					</div>
				);
				break;
			case "altrow":
				loopContent = (
					<div className={className}>
						{
							results.map((item, i) => {
								return (
									i % 2 == 0 ?
										<div className={'row loop-item loop-item-' + i} key={i}>
											{renderFunc(item, i, results.length)}
										</div> :
										<div className={'row row-alt loop-item loop-item-' + i} key={i}>
											{renderFunc(item, i, results.length)}
										</div>
								);
							})
						}
					</div>
				);
				break;
			default:
				loopContent = (
					<div className={className}>
						{
							results.map((item, i) => {
								return (
									<div className={'loop-item loop-item-' + i + ' ' + (this.props.subClassName ? this.props.subClassName : '')} key={i}>
										{renderFunc(item, i, results.length)}
									</div>
								);
							})
						}
					</div>
				);
		}
		
		var pageCfg = this.props.paged;
		
		if(!pageCfg){
			return loopContent;
		}
		
		var pageSize = pageCfg.pageSize || DEFAULT_PAGE_SIZE;
		var showInput = pageCfg.showInput !== undefined ? pageCfg.showInput : undefined;
		var maxLinks = pageCfg.maxLinks || undefined;

		if(typeof pageCfg == "number"){
			pageSize = pageCfg;
		}
		
		// if filter contains pagesize use that
		if (this.props.filter && this.props.filter.pageSize ) {
			pageSize = this.props.filter.pageSize;
		}

		// override with mobile pagesize if available
		if (typeof pageCfg == "object" && pageCfg.mobilePageSize) {

			if (window.matchMedia('(max-width: 752px) and (pointer: coarse) and (orientation: portrait)').matches ||
				window.matchMedia('(max-height: 752px) and (pointer: coarse) and (orientation: landscape)').matches) {
				pageSize = pageCfg.mobilePageSize;
			}

		}

		// Paginate
		var Module = pageCfg.module || Paginator;
		
		var paginator = <Module
			pageSize={pageSize}
			showInput={showInput}
			maxLinks={maxLinks}
			pageIndex={this.state.pageIndex}
			totalResults={this.state.totalResults}
			//scrollPref={this.props.scrollPref}
			onChange={pageIndex => {
				this.load(this.props, pageIndex);
				global.scrollTo && global.scrollTo(0, 0);
			}}
		/>;
		
		var result = [];
		
		if(pageCfg.top){
			result.push(paginator);
		}
		
		result.push(loopContent);
		
		if(pageCfg.bottom !== false){
			// Bottom is true unless it's explicitly false
			result.push(paginator);
		}
		
		return result;
	}
}

Loop.propTypes = {
	items: 'set',
	mode: ['table', 'columns', 'inline', 'bulletpoints', 'unformatted', 'altrows'],
	inGroupsOf: 'int'
};
Loop.icon = 'list';