
/*
* Content refs are handled by the frontend because they can also have custom handlers.
* for example, youtube:8hWsalYQhWk or fa:fa-user are valid refs too.
* This method converts a textual ref to either a HTML representation or a URL, depending on if you have options.url set to true.
* Specific ref protocols (like public: private: fa: youtube: etc) use the options as they wish.
*/

export default function getRef(ref, options) {
	var r = getRef.parse(ref);
	return r ? r.handler(r.ref, options || {}, r) : null;
}

function basicUrl(url, options){
	if(options.url){
		return url;
	}
	
	// React component by default:
	return (<img loading="lazy" src={url} {...options.attribs} />);
}

function staticFile(ref, options, r){
	var refParts = ref.split('/');
	var mainDir = refParts.shift();
	var cfg = global.config;
	var url = (cfg && cfg.pageRouter && cfg.pageRouter.hash ? 'pack/static/' : '/pack/static/') + refParts.join('/');
	if(mainDir.toLowerCase() == 'admin'){
		url = '/en-admin' + url;
	}
	
	url = (global.staticContentSource || '') + url;
	
	if(options.url){
		return url;
	}
	
	// React component by default:
	return (<img src={url} width={options.size || undefined} loading="lazy" {...options.attribs} />);
}

function contentFile(ref, options, r){
	var url = r.scheme == 'public' ? '/content/' : '/content-private/';
	
	var dirs = ref.split('/');
	ref = dirs.pop();
	if(options.dirs){
		dirs = dirs.concat(options.dirs);
	}
	
	var hadServer = false;
	
	if(dirs.length>0){
		// If dirs[0] contains . then it's a server address (for example, public:mycdn.com/123.jpg
		if(dirs[0].indexOf('.') != -1){
			var addr = dirs.shift();
			url = '//' + addr + url;
			hadServer = true;
		}
		
		url += dirs.join('/') + '/';
	}
	
	if(!hadServer && global.contentSource){
		url = global.contentSource + url;
	}
	
	var fileParts = ref.split('.');
	var id = fileParts.shift();
	var type = fileParts.join('.');
	
	var video = (type == 'mp4' || type == 'ogg' || type == 'webm' || type == 'avi');
	
	url = url + id + '-' + ((video || type == 'svg' || type == 'apng' || type == 'gif') ? (options.videoSize || 'original') : (options.size || 'original')) + (options.sizeExt || '') + '.' + type;
	
	if(options.url){
		return url;
	}
	
	if(video){
		return (<video src={url} width={options.size || 256} loading="lazy" controls {...options.attribs} />);
	}
	
	// React component by default:
	return (<img src={url} width={options.size || undefined} loading="lazy" {...options.attribs} />);
}

function fontAwesomeIcon(ref, options, r){
	// Note: doesn't support options.url (yet!)
	if(options.url){
		return '';
	}

	// allows us to specify FontAwesome modifier classes, e.g. fa-fw
	var className = r.scheme + ' ' + ref;

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
	's': staticFile,
	'url': basicUrl,
	'http': basicUrl,
	'https': basicUrl,
	'fa': fontAwesomeIcon,
	'fas': fontAwesomeIcon,
	'far': fontAwesomeIcon,
	'fad': fontAwesomeIcon,
	'fal': fontAwesomeIcon,
	'fab': fontAwesomeIcon,
	'fr': fontAwesomeIcon, // Four Roads icon fonts use "fr" as opposed to "fa"
	'emoji': emojiStr
};

getRef.parse = (ref) => {
	if(!ref){
		return null;
	}
	if(ref.scheme){
		return ref;
	}
	var protoIndex = ref.indexOf(':')
	var scheme = (protoIndex == -1) ? 'https' : ref.substring(0, protoIndex);
	var handler = protocolHandlers[scheme];
	
	if(!handler){
		return null;
	}
	
	ref = protoIndex == -1 ? ref : ref.substring(protoIndex+1);
	var fileParts = null;
	var fileType = null;
	
	if(ref.indexOf('.') != -1){
		fileParts = ref.split('.');
		fileType = fileParts.pop();
	}
	
	var refInfo = {
		scheme,
		handler,
		ref,
		fileType,
		fileParts
	};
	
	refInfo.toString = () => {

		if (refInfo.fileParts === null) {
			return null;
		}

		return refInfo.scheme + ':' + refInfo.fileParts.join('.') + '.' + refInfo.fileType;
	};
	
	return refInfo;
};

/*
* Convenience method for identifying visual refs (including videos).
*/
var imgTypes = ['png', 'jpeg', 'jpg', 'gif', 'mp4', 'svg', 'bmp', 'apng', 'avif', 'webp'];
var vidTypes = ['mp4', 'webm', 'avif'];

getRef.isImage = (ref) => {
	var info = getRef.parse(ref);
	if(!info){
		return false;
	}
	
	if(info.scheme == 'private'){
		return false;
	}else if(info.scheme == 'url' || info.scheme == 'http' || info.scheme == 'https' || info.scheme == 'public'){
		return (imgTypes.indexOf(info.fileType) != -1);
	}
	
	// All other ref types are visual:
	return true;
}

getRef.isVideo = (ref) => {
	var info = getRef.parse(ref);
	if(!info){
		return false;
	}
	
	if(info.scheme == 'private'){
		return false;
	}else if(info.scheme == 'url' || info.scheme == 'http' || info.scheme == 'https' || info.scheme == 'public'){
		return (vidTypes.indexOf(info.fileType) != -1);
	}
	
	return false;
}