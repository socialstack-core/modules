
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
	
	var dirs = ref.split('/');
	ref = dirs.pop();
	if(options.dirs){
		dirs = dirs.concat(options.dirs);
	}
	if(dirs.length>0){
		// If dirs[0] contains . then it's a server address (for example, public:mycdn.com/123.jpg
		if(dirs[0].indexOf('.') != -1){
			var addr = dirs.shift();
			url = '//' + addr + url;
		}
		
		url += dirs.join('/') + '/';
	}
	
	var fileParts = ref.split('.');
	var id = fileParts.shift();
	var type = fileParts.join('.');
	
	var video = (type == 'mp4' || type == 'ogg' || type == 'webm' || type == 'avi');
	
	url = url + id + '-' + (video ? 'original' : (options.size || 'original')) + '.' + type;
	
	if(options.url){
		return url;
	}
	
	if(video){
		return (<video src={url} width={300} controls {...options.attribs} />);
	}
	
	// React component by default:
	return (<img src={url} {...options.attribs} />);
}

function fontAwesomeIcon(ref, options){
	// Note: doesn't support options.url (yet!)
	if(options.url){
		return '';
	}

	// allows us to specify FontAwesome modifier classes, e.g. fa-fw
	var className = options.protocol + ' ' + ref;

	if (options.className) {
		className += ' ' + options.className;
	}

	return (<i className={className} {...options.attribs}/>);
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