import GraphNode from 'Admin/CanvasEditor/GraphEditor/GraphNode';
import Input from 'UI/Input';
import EventSelect, {getAllEvents} from '../EventSelect';


/*
* Defines the admin UI handler for the EventTrigger graph node.
* This is separate from the graph node executor which exists on the frontend.
*/
export default class EventTrigger extends GraphNode {
	
	constructor(props){
		super(props);
	}
	
	async validateState() {
		var { eventName } = this.state;
		
		if(!eventName || eventName == this.eNameFieldsFor){
			return;
		}
		
		this.eNameFieldsFor = eventName;
		var allEvents = await getAllEvents();
		
		// Get the data for this event by its name.
		var eventData = allEvents.events.find(evt => evt.fullName == eventName);
		this.eventData = eventData;
	}
	
	renderFields() {
		
		var fields = [
			{
				key: 'eventName',
				name: `Event`,
				type: 'eventName',
				onRender: (value, onSetValue, label) => {
					return <EventSelect label={label} value={value} onChange={e => {
						var typeName = e.target.value;
						onSetValue(typeName);
					}} />
				},
				direction: 'none'
			}
		];
		
		var eventName = this.state.eventName;
		
		if(eventName && this.eventData && this.eventData.parameters){
			// We've got an event selected. 
			
			var usedKeys = {};
			
			this.eventData.parameters.forEach((pType, index) => {
				
				var key = pType.shortName.toLowerCase();
				var name = pType.shortName;
				
				var usedCount = usedKeys[key] || 0;
				
				if(usedCount){
					key += '_' + (usedCount+1);
					
					if(eventName.indexOf('.BeforeUpdate') != -1 && index == 2){
						name = `Original value`;
					}else{
						name += ' ' + (usedCount + 1);
					}
				}
				
				usedKeys[key] = usedCount + 1;
				
				fields.push({
					key,
					type: {name: pType.name, isArray: pType.isArray},
					name,
					direction: 'out'
				});
				
			});
			
			fields.push({
				key: 'thenDo',
				type: 'execute',
				name: `Then`,
				direction: 'out'
			});
			
		}
		
		return fields;
	}
	
}