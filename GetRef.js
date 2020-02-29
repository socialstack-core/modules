
/*
* Content refs are handled by the frontend because they can also have custom handlers.
* for example, youtube:8hWsalYQhWk or fa:fa-user are valid refs too.
* This method converts a textual ref to either a HTML representation or a URL, depending on if you have options.url set to true.
* Specific ref protocols (like public: private: fa: youtube: etc) use the options as they wish.
*/

export default function getRef(ref, options) {
	if(!ref){
		return null;
	}
	var protoIndex = ref.indexOf(':')
	
	if(protoIndex == -1){
		return null;
	}
	
	var proto = ref.substring(0, protoIndex);
	var handler = protocolHandlers[proto];
	if(!handler){
		return null;
	}
	
	options = options || {};
	options.protocol = proto;
	ref = ref.substring(protoIndex+1);
	return handler(ref, options);
}

function basicUrl(url, options){
	if(options.url){
		return url;
	}
	
	// React component by default:
	return (<img src={url} {...options.attribs} />);
}

function contentFile(ref, options){
	var url = options.protocol == 'public' ? '/content/' : '/content-private/';
	
	var serverParts = ref.split('/');
	
	if(serverParts.length>1){
		url = '//' + serverParts[0] + url;
		ref = serverParts[serverParts.length-1];
	}
	
	var fileParts = ref.split('.');
	var id = fileParts.shift();
	var type = fileParts.join('.');
	url = url + id + '-' + (options.size || 'original') + '.' + type;
	
	if(options.url){
		return url;
	}
	
	if(type == 'mp4' || type == 'ogg' || type == 'webm'){
		return (<video src={url} {...options.attribs} />);
	}
	
	// React component by default:
	return (<img src={url} {...options.attribs} />);
}

function fontAwesomeIcon(ref, options){
	// Note: doesn't support options.url (yet!)
	if(options.url){
		return '';
	}
	return (<i className={options.protocol + ' ' + ref} {...options.attribs}/>);
}

function emojiStr(ref, options){
	// Note: doesn't support options.url (yet!)
	if(options.url){
		return '';
	}
	var emojiString = String.fromCodePoint.apply(String, ref.split(',').map(num => parseInt('0x' + num)));
	return (<span className="emoji" {...options.attribs}>{emojiString}</span>);
}

var protocolHandlers = {
	'public': contentFile,
	'private': contentFile,
	'url': basicUrl,
	'fa': fontAwesomeIcon,
	'far': fontAwesomeIcon,
	'fad': fontAwesomeIcon,
	'fal': fontAwesomeIcon,
	'emoji': emojiStr
};