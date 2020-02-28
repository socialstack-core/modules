import Loading from 'UI/Loading';
import Input from 'UI/Input';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';

var eventList = null;
var loadingEventList = null;

/**
 * Used to select an event from the API.
 * Displays the event name.
 * Can alternatively provide a props.onDisplay(evt, isSuggestion) method which renders the event rows as you wish.
 */
export default class EventSelect extends React.Component {
	constructor(props){
		super(props);
		this.state = {
			loading: false,
			searchMode: true,
			events: null
		};
	}
	
	splitName(label){
		// helloWorld -> Hello World
		label = label.replace(/([^A-Z])([A-Z])/g, '$1 $2');
		label = label[0].toUpperCase() + label.substring(1);
		return label;
	}
	
    componentDidMount(){
        var event = this.props.event;
        if(event){
            this.setState({
                event,
                searchMode: false
            });
        }else{
			var name = this.props.value || this.props.defaultValue;
			
			if(!name){
				return;
			}
			
			this.setState({
				event: {name, niceName: this.splitName(name), lowerName: name.toLowerCase()},
				searchMode: false
			});
		}
    }
    
	selectEvent(event){
		this.setState({
			events: null,
			searchMode: false,
			event
		});
		
		this.props.onChange && this.props.onChange(event);
	}
	
	search(query){
		query = query.toLowerCase();
		
		if(!eventList && !loadingEventList){
			this.setState({loading: true});
			loadingEventList = webRequest('apievent/list').then(response => {
				eventList = response.json.results;
				loadingEventList = null;
				
				eventList.forEach(evt => {
					evt.niceName = this.splitName(evt.name);
					evt.lowerName = evt.name.toLowerCase();
				});
				this.setState({loading: false, events: eventList.filter(result => result.lowerName.indexOf(query) != -1)});
			});
		}else{
			this.setState({events: eventList.filter(result => result.lowerName.indexOf(query) != -1)});
		}
		
	}
	
	display(event, isSuggestion){
		if(this.props.onDisplay){
			return this.props.onDisplay(event, isSuggestion);
		}
		
		return event.niceName;
	}
	
	renderInput(){
		return [
			this.state.searchMode ? (
				<div>
					<input autoComplete="false" className="form-control" placeholder="Find an event.." type="text" name={"__event_search_" + (this.props.name || "eventName")} onKeyUp={(e) => {
						this.search(e.target.value);
					}}/>
					{this.state.events && (
						<div className="suggestions">
							{this.state.events.length ? (
								this.state.events.map((event, i) => (
									<div key={i} onClick={() => this.selectEvent(event)} className="suggestion">
										{this.display(event, true)}
									</div>
								))
							) : (
								<div>
									No events found
								</div>
							)}
						</div>
					)}
				</div>
			) : (
				<div>
					{this.state.event && this.display(this.state.event, false)} <div style={{marginLeft: '15px'}} className="btn btn-secondary" onMouseDown={() => this.setState({searchMode: true})}>Change</div>
				</div>
			),
			<input type="hidden" value={(this.state.event && this.state.event.name) || this.props.value || this.props.defaultValue} name={this.props.name || "eventName"} />	
		];
	}
	
	render() {
		return <Input {...this.props} type={() => this.renderInput()}/>;
	}
}