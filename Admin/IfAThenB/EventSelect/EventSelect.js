import Input from 'UI/Input';
import webRequest from 'UI/Functions/WebRequest';
import {niceName} from 'Admin/CanvasEditor/GraphEditor/Utils';

var _allEvents = null;

export function getAllEvents() {
	
	if(_allEvents){
		return Promise.resolve(_allEvents);
	}
	
	return webRequest('athenb/event-list').then(response => {
		
		var types = response.json.types;
		var evts = response.json.results;
		
		var typeless = [];
		var typeLookup = {};
		
		types.forEach(type => {
			type.events = [];
			type.shortName = type.name;
			
			var arrOrGen = type.name.indexOf('[');
			
			if(arrOrGen == -1){
				arrOrGen = type.name.indexOf('<');
				
				if(arrOrGen != -1){
					type.shortName = type.name.substring(0, arrOrGen);
				}
			}else{
				type.isArray = true;
				type.shortName = type.name.substring(0, arrOrGen);
			}
			
			typeLookup[type.name.toLowerCase()] = type;
		});
		
		evts.forEach(evt => {
			var eventNameParts = evt.name.split('.');
			evt.fullName = evt.name;
			evt.name = eventNameParts[eventNameParts.length - 1];
			
			if(eventNameParts.length == 2){
				var onType = typeLookup[eventNameParts[0].toLowerCase()];
				
				if(onType){
					evt.onType = onType;
					onType.events.push(evt);
				}
			}else{
				typeless.push(evt);
			}
			
			evt.parameters = evt.parameterTypes.map(typeIndex => typeIndex < 0 || typeIndex >= types.length ? null : types[typeIndex]);
		});
		
		_allEvents = {
			events: evts,
			types,
			typeLookup,
			typeless
		};
		return _allEvents;
	});
}

function ContentTypeSelect(props){
	
	var options = [<option key={'_'} value={''}>Select one..</option>];
	
	for(var contentTypeKey in props.eventInfo.types){
		var typeInfo = props.eventInfo.types[contentTypeKey];
		
		if(!typeInfo.events || !typeInfo.events.length){
			continue;
		}
		
		options.push(<option key={contentTypeKey} value={typeInfo.name}>{niceName(typeInfo.name)}</option>);
	}
	
	return <Input type='select' name={props.name} value={props.value} defaultValue={props.value} onChange={props.onChange}>
		{options}
	</Input>
}

export default function EventSelect(props){
	
	var [eventInfo, setEventInfo] = React.useState(null);
	var [contentTypeOverride, setContentType] = React.useState(null);
	
	React.useEffect(() => {
		
		getAllEvents().then(ct => {
			setEventInfo(ct);
		});
		
	}, []);
	
	if(!eventInfo){
		return 'Getting event data..';
	}
	
	var name = props.value || props.defaultValue;
	
	var contentType = null;
	
	if(name && name.indexOf('.') != -1){
		contentType = name.substring(0, name.indexOf('.'));
	}
	
	if(contentTypeOverride){
		contentType = contentTypeOverride;
	}
	
	var typeInfo = contentType ? eventInfo.typeLookup[contentType.toLowerCase()] : null;
	
	var options = [<option key={'_'} value={''}>Select one..</option>];
	
	var events = typeInfo ? typeInfo.events.map(evt => <option key={evt.fullName} value={evt.fullName}>
		{niceName(evt.name)}
	</option>) : [];
	
	return <>
			<ContentTypeSelect eventInfo={eventInfo} value={contentType} defaultValue={contentType} onChange={(e) => {
				console.log(e.target.value, e);
				setContentType(e.target.value);
			}} />
			{typeInfo && <Input type='select' name={props.name} value={props.value} defaultValue={props.value} onChange={props.onChange}>
				{events}
			</Input>}
	</>;
}
