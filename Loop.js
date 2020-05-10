import webRequest from 'UI/Functions/WebRequest';
import Column from 'UI/Column';
import Row from 'UI/Row';
import webSocket from 'UI/Functions/WebSocket';
import getEndpointType from 'UI/Functions/GetEndpointType';
import Failure from 'UI/Failed';

// Operators as used by Filter in where clauses.
const filterOperators = {
	"startsWith": (a,b) => a && a.startsWith(b),
	"contains": (a,b) => a && a.indexOf(b) != -1,
	"endsWith": (a,b) => a && a.endsWith(b),
	"geq": (a,b) => a >= b,
	"greaterThanOrEqual": (a,b) => a >= b,
	"greaterThan": (a,b) => a > b,
	"lessThan": (a,b) => a < b,
	"leq": (a,b) => a <= b,
	"lessThanOrEqual": (a,b) => a <= b,
	"not": (a,b) => a != b,
	"equals":(a,b) => a == b
};

/**
 * This component repeatedly renders its child using either an explicit array of data or an endpoint.
 */
export default class Loop extends React.Component {
	
	
	
	constructor(props) {
		super(props);
		this.state = {
			results: null,
			errored: false
		};
		
		this.onLiveMessage = this.onLiveMessage.bind(this);
		this.onContentChange = this.onContentChange.bind(this);
	}
	
	onLiveMessage(msg){
		if(msg.type == "status"){
			if(msg.connected){
				// Force a reload:
				this.load(this.props, true);
			}
			
			this.props.onLiveStatus && this.props.onLiveStatus(msg.connected);
			
			return;
		}
		
		// Push msg.entity into the results set:
		if(this.state.results && msg.entity){
			var e = msg.entity;
			
			if(msg.by && e.viewedAtUtc){
				// Special views specific functionality here.
				// If we receive an update via the websocket, we must change its viewedAtUtc field (if it has one).
				// That's because its value is user specific, and is set to the value of the person who raised the event.
				// Lots of database traffic just isn't worthwhile given the UI can figure it out for itself.
				
				// If *this user* made the update, set the viewed date as the edited date.
				// Otherwise, clear it. We don't know when this user actually last saw it.
				var { user } = global.app.state;
				
				var userId = user ? user.id : 0;
				
				if(msg.by == userId){
					e.viewedAtUtc = e.editedUtc;
				}else{
					e.viewedAtUtc = null;
				}
			}
			
			var entityId = e.id;
			
			if(msg.method == 'delete'){
				// Remove by id:
				this.setState({results: this.state.results.filter(ent => ent && ent.id != entityId)});
			}else if(msg.method == 'update'){
				// find by id:
				
				var res = this.state.results;
				var found = false;
				for(var i=0;i<res.length;i++){
					if(!res[i]){
						continue;
					}
					
					if(res[i].id == entityId){
						res[i] = e;
						found = true;
					}
				}
				
				if(found){
					// If it still passes the filter, keep it. Otherwise, delete it.
					var keep = this.testFilter(e);
					
					if(keep && !this.isSorted()){
						this.setState({results: res});
					}else{
						// Delete it:
						res = res.filter(ent => ent && ent.id != entityId);
						
						if(keep){
							// Re-add it (into a sorted list). This allows an updated entity to move change order.
							this.add(e, res);
						}else{
							this.setState({results: res});
						}
					}
				}else{
					
					// Does it pass the filter? If it does, add it.
					if(this.testFilter(e)){
						this.add(e, res);
					}
					
				}
				
			}else if(msg.method == 'create'){
				
				// already in there?
				var results = this.state.results;
				
				if(!results.find(ent => ent && ent.id == entityId)){
					
					// Nope - potentially adding it.
					// First though, make sure it passes the filter if there is one.
					if(this.testFilter(msg.entity)){
						this.add(msg.entity, results);
					}
				}
				
			}
			
		}
	}
	
	// Finds the index where the given item should be inserted.
	sortedSearch(arr, item, compare, low, high){ 
		if (high <= low){
			return compare(item, arr[low]) == 1 ? (low + 1): low; 
		}
		
		// Mid coerced to int:
		var mid = ((low + high)/2)|0; 
		
		if(compare(item, arr[mid]) == 0){
			return mid+1; 
		}
		
		if(compare(item, arr[mid]) == 1){
			return this.sortedSearch(arr, item, compare, mid+1, high);
		}
		return this.sortedSearch(arr, item, compare, low, mid-1); 
	} 
	
	isSorted(){
		var { filter } = this.props;
		return filter && filter.sort && filter.sort.field;
	}
	
	add(entity, results){
		
		var { filter } = this.props;
		
		if(results.length && this.isSorted()){
			// account for the sort too.
			
			// Ascending is default.
			var desc = (filter.sort.dir || filter.sort.direction) == 'desc';
			
			var positive = desc ? -1 : 1;
			var negative = -positive;
			
			// The sort field may have an uppercase first letter:
			var fieldName = filter.sort.field;
			fieldName = fieldName.charAt(0).toLowerCase() + fieldName.slice(1);
			
			// Read the field:
			var fieldValue = entity[fieldName];
			
			// Date field? Special case for those:
			var isDate = fieldName.endsWith('Utc');
			
			if(isDate){
				// Load str:
				fieldValue = fieldValue ? new Date(fieldValue).getTime() : null;
			}
			
			// Search for the target index:
			var targetIndex = this.sortedSearch(results, fieldValue, (a, b) => {
				// a is the field value from the entity.
				// b is the object to compare with.
				var bField = b ? b[fieldName] : null;
				
				if(isDate){
					// Parse the date:
					if(typeof bField == 'string'){
						bField = new Date(bField).getTime();
					}else if(bField && bField.getTime){
						bField = bField.getTime();
					}
				}
				
				if(a > bField){
					return positive;
				}
				
				return (a == bField) ? 0 : negative;
			}, 0, results.length-1);
			
			// Push it in:
			results.splice(targetIndex, 0, entity);
			
		}else{
			results.push(entity);
		}
		
		this.props.onLiveCreate && this.props.onLiveCreate(entity);
		this.setState({results});
	}
	
	testFilter(ent){
		
		var {filter} = this.props;
		
		if(filter && filter.where){
			
			// Simple filter fields for now.
			// Doesn't support the advanced loop filtering.
			for(var key in filter.where){
				if(!key || !key.length){
					continue;
				}
				
				var value = filter.where[key];
				
				// Lowercase the key as it's an exact field match:
				var entityKeyName = key.charAt(0).toLowerCase() + key.slice(1);
				
				var reqValue = ent[entityKeyName];
				
				// value can be an array of options:
				if(Array.isArray(value)){
					// Failed the filter if the reqd value is not in the array.
					return value.indexOf(reqValue) != -1;
				}else if(value && typeof value == 'object'){
					// Potentially defining an operator other than equals.
					// {not: 'test'} etc.
					for(var filterField in filterOperators){
						var fValue = value[filterField];
						
						// Basic check to make sure it's not a function.
						// - It can be null though
						if(fValue !== undefined && (fValue === null || typeof fValue != 'object')){
							
							// Use this operator:
							if(!filterOperators[filterField](reqValue, fValue)){
								return false;
							}
							
						}
					}
				}
				
				if(reqValue != value){
					return false;
				}
			}
			
		}
		
		return true;
	}
	
	onContentChange(e){
		
		// Content changed! Is it relevant to this loop?
		var results = this.state.results;
		
		if(typeof this.props.over != 'string' || !results){
			// Not looping an endpoint OR we don't have any results yet anyway.
			return;
		}
		
		var endpoint = e.endpoint;
		
		if(this.props.updateContentType){
			// If you're using custom endpoints, specify this updateContentType prop to be able to still receive live updates.
			
			if(this.props.updateContentType != e.entity.type){
				// This content isn't of the same type as this loop.
				return;
			}
			
		}else if(getEndpointType(this.props.over).type != e.endpointType){
			// This content isn't of the same type as this loop.
			return;
		}
		
		var entity = e.entity;
		
		if(this.props.onContentChange){
		
			entity = this.props.onContentChange(entity);
		
			if(!entity){
				// Handler rejected
				return;
			}
		}
		
		if(e.deleted){
			// Remove it from the results if we have it.
			results = results.filter(c => c.id != entity.id);
		}else{
			
			// Update or add. 
			// Already got this content?
			var exists = false;
			
			for(var i=0;i<results.length;i++){
				if(results[i].id == entity.id){
					// Update.
					results[i] = entity;
					exists = true;
				}
			}
			
			if(!exists){
				// TODO: Only do so if entity fulfils our filter.
				if(this.props.filter){
					console.log('TODO: Ensure entity fulfils the filter.');
				}
				results.push(entity);
			}
		}
		
		this.setState({results});
	}
	
	componentWillMount(){
		if(this.props.live){
			webSocket.addEventListener(this.props.live, this.onLiveMessage);
		}
		
		// contentchange is fired off by posting to API endpoints which then return an entity (object with both id and type fields).
		document.addEventListener("contentchange", this.onContentChange);
		
		this.load(this.props);
	}
	
	componentWillUnmount(){
		if(this.props.live){
			webSocket.removeEventListener(this.props.live, this.onLiveMessage);
		}
		
		document.removeEventListener("contentchange", this.onContentChange);
	}
	
	componentWillReceiveProps(props) {
		this.load(props);
	}
	
	load(props, force) {
		if(typeof props.over == 'string'){
			
			var jsonFilter = props.filter ? JSON.stringify(props.filter) : null;
			
			if(!force && this.state.over == props.over && jsonFilter == this.state.jsonFilter){
				// Avoid making a new request.
				return;
			}
			
			this.setState({over: props.over, jsonFilter, results: null});
			if(!this.state.errored){
				webRequest(props.over, props.filter).then(response => {
					var fieldName = props.field || 'results';
					var results = (response && response.json && response.json[fieldName]) ? response.json[fieldName] : [];
					
					if(props.onResults){
						results = this.props.onResults(results);
					}
					
					this.setState({results});
				}).catch(e => {
					console.error(e);
					
					var results = [];
					
					if(props.onResults){
						results = this.props.onResults(results);
					}
					
					this.setState({over: null, jsonFilter: null, results, errored: true});
				});
			}

		}else{
			// Direct array:
			var results = props.over;
			
			if(props.onResults){
				results = this.props.onResults(results);
			}
			
			this.setState({over: null, jsonFilter: null, results});
		}
	}
	
	render() {
		if (!this.state.results && !this.props.items) {

			if(this.state.errored){
				// is a specific failure set?
				if(this.props.onFailure){
					if(typeof this.props.onFailure === "function"){
						return this.props.onFailure();
					}
				}
				return <Failure/>
			}
			
			// Loading
			if(this.props.loader){
				
				if (typeof this.props.loader === "function") {
					return this.props.loader();
				}else{
					return 'Loading...';
				}
				
			}

			return null;
		}
		
		var results = this.state.results;
		
		var renderFunc = this.props.children;
		
		if(this.props.items){
			results = this.props.items;
			
			// Use the provided renderer instead:
			var Module = results.renderer;
			
			renderFunc = (item, i, count) => {
				return <Module item={item} container={this.props}/>
			};
		}
		
		if(!results.length){
			// No results at all.
			var M = this.props.noneDisplayer;
			if(M){
				return <M loopAllProps={this.props} />;
			}
			
			return this.props.orNone ? this.props.orNone() : null;
		}
		
		var className = 'loop ' + (this.props.name ? this.props.name : ((typeof this.props.over == 'String') ? this.props.over.replace('/', '-') : ''));
		if(this.props.className)
		{
			var className = this.props.className;
		}
		
		if(this.props.groupAll){
			// Call the child method just once with the results as a single block.
			results = [results];
		}
		
		if(this.props.inGroupsOf && this.props.inGroupsOf > 0){
			// Group up results into blocks of x entries.
			var newResults = [];
			var groupsOf = this.props.inGroupsOf;
			for(var i=0;i<results.length;i+=groupsOf){
				newResults.push(results.slice(i, i+groupsOf));
			}
			results = newResults;
		}
		
		var mode = this.props.mode;
		
		if(this.props.colMd){
			mode = "colmd";
		}else if(this.props.altRow){
			mode = "altrow";
		}else if(this.props.asTable){
			mode = "table";
		}else if(this.props.asUl){
			mode = "ul";
		}else if(this.props.asCols){
			mode = "cols";
		}else if(this.props.raw){
			mode = "raw";
		}else if(this.props.inline){
			mode = "inline";
		}
		
		switch(mode){
		case "inline":
			return (
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
		case "unformatted":
			return (<span className={className}>
				{results.map((item, i) => {
					return renderFunc(item, i, results.length);
				})}
				</span>
			);
		break;
		case "cols":
		case "columns":
			var size = parseInt(this.props.size);
			
			if(!size || isNaN(size)){
				size=4;
			}
			
			if(size<=0){
				size=1;
			}else if(size>12){
				size=12;
			}
			
			var colCount = Math.floor(12/size);
			var rowCount = Math.ceil(results.length / colCount);
			var col=0;
			var rows = [];
			
			for(var r=0;r<rowCount;r++){
				var cols = [];
				
				for(var c=0;c<colCount;c++){
					if(col>=results.length){
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
			
			return <div className={className}>{rows}</div>;
		break;
		case "ul":
		case "bulletpoints":
			return (
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
			var bodyFunc = renderFunc;
			var footerFunc = null;
			
			if(renderFunc.length && !this.props.items){
				if(renderFunc.length == 1){
					bodyFunc = renderFunc[0];
				}else{
					headerFunc = renderFunc[0];
					bodyFunc = renderFunc[1];
					
					if(renderFunc.length > 1){
						footerFunc = renderFunc[1];
					}
				}
			}
			
			return (
				<table className={"table " + className}>
					{headerFunc && (
						<thead>
						{
							headerFunc(results)
						}
						</thead>
					)}
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
					{footerFunc && (
						<tfoot>
						{
							footerFunc(results)
						}
						</tfoot>
					)}
				</table>
			);
		break;
		case "colmd":
			if(this.props.colMd > 0){
				return (
					<div className={className}>
					{
						results.map((item, i) => {
							return (
								<div className={'col-md-'+this.props.colMd+' loop-item loop-item-' + i} key={i}>
									{renderFunc(item, i, results.length)}
								</div>
							);
						})
					}
					</div>
				);
			}
		break;
		case "altrow":
			return (
				<div className={className}>
				{
					results.map((item, i) => {
						return (
							i%2==0?
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
		}
		
		return (
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
}

Loop.propTypes={
	items: 'set',
	mode: ['table','columns', 'inline', 'bulletpoints','unformatted','altrows'],
	inGroupsOf: 'int'
};
Loop.icon='list';