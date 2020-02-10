import webRequest from 'UI/Functions/WebRequest';
import Column from 'UI/Column';
import Row from 'UI/Row';
import webSocket from 'UI/Functions/WebSocket';
import getEndpointType from 'UI/Functions/GetEndpointType';
import Failure from 'UI/Failed';

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
			this.props.onLiveStatus && this.props.onLiveStatus(msg.connected);
			return;
		}
		
		// Push msg.entity into the results set:
		if(this.state.results && msg.entity){
			this.state.results.push(msg.entity);
			// State nudge:
			this.setState({results: this.state.results});
		}
		console.log(msg);
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
	
	load(props) {
		if(typeof props.over == 'string'){
			
			var jsonFilter = props.filter ? JSON.stringify(props.filter) : null;
			
			if(this.state.over == props.over && jsonFilter == this.state.jsonFilter){
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
			var results = props.from;
			
			if(props.onResults){
				results = this.props.onResults(results);
			}
			
			this.setState({over: null, jsonFilter: null, results});
		}
	}
	
	render() {
		if (!this.state.results) {

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
		
		if(!this.state.results.length){
			// Any results?
			return this.props.orNone ? this.props.orNone() : null;
		}

		var className = 'loop ' + (this.props.name ? this.props.name : ((typeof this.props.over == 'String') ? this.props.over.replace('/', '-') : ''));
		if(this.props.className)
		{
			var className = this.props.className;
		}

		var results = this.state.results;
		
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
		
		var renderFunc = this.props.children;
		
		if(this.props.inline){
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
		}
		
		if(this.props.raw){
			return (<span className={className}>
				{results.map((item, i) => {
					return renderFunc(item, i, results.length);
				})}
				</span>
			);
		}
		
		if(this.props.asCols){
			
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
		}
		
		if(this.props.asUl){
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
		}
		
		if(this.props.asTable){
			return (
				<table className={"table " + className}>
					<tbody>
					{
						results.map((item, i) => {
							return (
								<tr className={'loop-item loop-item-' + i} key={i}>
									{renderFunc(item, i, results.length)}
								</tr>
							);
						})
					}
					</tbody>
				</table>
			);
		}

		if(this.props.colMd ){
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
		}

		if(this.props.altRow){
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