import getEndpointType from 'UI/Functions/GetEndpointType';

/*
* Trigger a content change event for the given entity.
* Either it was edited, or is new.
*/
export default function contentChange(entity, endpoint, changeDetail){
	
	var e = document.createEvent('Event');
	e.initEvent('contentchange', true, true);
	
	var endpointInfo = getEndpointType(endpoint);
	
	e.endpointType = endpointInfo.type;
	
	if(changeDetail){
		if(changeDetail.deleted){
			e.deleted = true;
		}else if(changeDetail.updated){
			e.updated = true;
		}else if(changeDetail.added || changeDetail.created){
			e.created = e.added = true;
		}
	}else{
		// Figure out if it was an update or created by default.
		if(endpointInfo.isUpdate){
			e.updated = true;
			e.updatingId = endpointInfo.updatingId;
		}else{
			e.created = e.added = true;
		}
	}
	
	e.change = changeDetail || {updated: true};
	e.endpoint = endpoint;
	e.entity = entity;
	
	// Dispatch the event:
	document.dispatchEvent(e);
}