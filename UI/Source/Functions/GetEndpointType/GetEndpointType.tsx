import isNumeric from 'UI/Functions/IsNumeric';

/**
 * Holds basic information about a particular endpoint derived exclusively from the URL itself.
 */
interface EndpointType {

	/**
	 * The content type name.
	 */
	type: string,

	/**
	 * True if this is an update endpoint.
	 */
	isUpdate: boolean,

	/**
	 * If this is an update endpoint, the ID of the content being updated.
	 */
	updatingId: int | undefined,

	/**
	 * True if this is a list endpoint.
	 */
	isList: boolean

}

/**
* Gets the core endpoint type from either a full URL or just the endpoint URL.
* For example, 'site.com/v1/answer/list'  => 'answer',  'v1/answer/list'  => 'answer', 'v1/answer'  => 'answer'
* Similarly 'site.com/v1/forum/reply' => 'forum/reply'.
* Note that this does not handle custom endpoints such as 'forum/reply/blah'.
 * @param ep The endpoint URL.
 * @returns
 */
export default function GetEndpointType(ep : string) : EndpointType {
	var parts = ep.split('v1/');
	
	if(parts.length == 2){
		ep = parts[1];
	}
	
	if(ep[ep.length-1] == '/'){
		ep = ep.substring(0, ep.length-1);
	}
	
	if(ep[0] == '/'){
		ep = ep.substring(1);
	}
	
	parts = ep.split('/'); // ['answer'] for create or ['answer', '2'] for update
	
	// If it ends with a number, or it's "list", we have an update endpoint:
	var last = parts[parts.length-1];
	var isUpdate = isNumeric(last);
	var isList = (last == 'list');
	
	if(isList || isUpdate){
		parts.pop();
	}
	
	return {
		type: parts.join('/'),
		isUpdate,
		updatingId: isUpdate ? parseInt(last) as int : undefined,
		isList
	};
}