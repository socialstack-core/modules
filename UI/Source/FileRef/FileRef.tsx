import getConfig from 'UI/Config';

const MEDIA_TYPES = {
	image: {
		web: ['png', 'jpeg', 'jpg', 'jfif', 'gif', 'svg', 'apng', 'webp', 'avif'],
		all: ['png', 'jpeg', 'jpg', 'jfif', 'gif', 'svg', 'bmp', 'apng', 'webp', 'avif', 'heic', 'heif', 'tiff', 'tif']
	},
	video: {
		web: ['mp4', 'webm'],
		all: ['avi', 'wmv', 'ts', 'm3u8', 'ogv', 'flv', 'h264', 'h265', 'webm', 'ogg', 'mp4', 'mkv', 'mpeg', '3g2', '3gp', 'mov', 'media']
	},
	audio: {
		web: ['wav', 'mp3', 'm4a', 'aac', 'flac'],
		all: ['wav', 'aiff', 'au', 'pcm', 'flac', 'ape', 'wv', 'alac', 'mp3', 'm4a', 'aac', 'ogg', 'opus', 'wma', 'amr', 'mid', 'midi']
	},
	document: {
		web: ['pdf', 'txt', 'csv', 'xml', 'html', 'json'],
		all: ['doc', 'docx', 'pdf', 'txt', 'rtf', 'pages', 'xls', 'xlsx', 'csv', 'ods', 'fods', 'odt', 'fodt', 'odp', 'fodp', 'odg', 'fodg', 'ppt', 'pptx', 'key', 'xml', 'html', 'json', 'md']
	}
};

export const imageTypes = MEDIA_TYPES.image.web;
export const allImageTypes = MEDIA_TYPES.image.all;

export const videoTypes = MEDIA_TYPES.video.web;
export const allVideoTypes = MEDIA_TYPES.video.all;

export const audioTypes = MEDIA_TYPES.audio.web;
export const allAudioTypes = MEDIA_TYPES.audio.all;

export const documentTypes = MEDIA_TYPES.document.web;
export const allDocumentTypes = MEDIA_TYPES.document.all;

/*
* Processes a socialstack URL-like string called a file ref.
* Content refs are handled by the frontend because they can also have custom handlers.
* for example, youtube:8hWsalYQhWk or fa:fa-user are valid refs too.
* This method converts a textual ref to either a HTML representation or a URL, depending on if you have options.url set to true.
* Specific ref protocols (like public: private: fa: youtube: etc) use the options as they wish.
*/
export function getUrl(ref : FileRefIsh, options? : FileRefOptions) {
    var r = parse(ref);

    if (!options) {
        options = {};
    }

    return r && r.handler ? r.handler(r, options) : undefined;
}

/**
 * Options used when constructing URLs for a fileRef.
 */
interface FileRefOptions {
    /**
     * Optional size. "original" is assumed otherwise.
     */
    size?: string;

    /**
     * Use this if you want a size containing a dot but do not want to lose the fileRefs type.
     */
    sizeExt?: string;

    /**
     * Force a webp type if present.
     */
    forceImage?: boolean;

    /**
     * Set this to the ideally wanted file type. It will be used if it is present.
     */
    ideal?: string;

    /**
     * Optionally push extra directories in to the URL whilst it is being constructed.
     */
    additionalDirectories?: string[]
}

/**
 * Handles http/s URLs.
 * @param refInfo
 * @param options
 * @returns
 */
function basicUrl(refInfo : FileRefInfo, options: FileRefOptions) {
    // strip out any leading slashes from url
    var qualifiedUrl = refInfo.scheme + '://' + refInfo.ref.replace(/^\/+/, '');

    if (options.size) {
        let fileIndex = refInfo.file ? qualifiedUrl.lastIndexOf(refInfo.file) : -1;

        if (fileIndex != -1 && refInfo.fileName?.endsWith("-original")) {
            qualifiedUrl = `${qualifiedUrl.slice(0, fileIndex)}${refInfo.fileName.replace('-original', '-' + options.size)}.${refInfo.fileType}`;
        }
    }

    return qualifiedUrl;
}

type PageRouterConfig = {
    hash?: string
};

/**
 * Handles files present in the source tree.
 * @param refInfo
 * @param options
 * @returns
 */
function staticFile(refInfo: FileRefInfo, options: FileRefOptions) {
    var refParts = refInfo.basepath.split('/');
    var mainDir = refParts.shift();
	var cfg = getConfig<PageRouterConfig>("PageRouter");

	const prefix = cfg?.[0]?.hash ? "pack/static/" : "/pack/static/";
	var url = prefix + refParts.join("/");

    if (mainDir?.toLowerCase() == 'admin') {
        url = '/en-admin' + url;
    }

    url = (window.staticContentSource || '') + url;

    if (options.size) {
        let fileIndex = refInfo.file ? url.lastIndexOf(refInfo.file) : -1;

        if (fileIndex != -1 && refInfo.fileName?.endsWith("-original")) {
            url = `${url.slice(0, fileIndex)}${refInfo.fileName.replace('-original', '-' + options.size)}.${refInfo.fileType}`;
        }
    }

    return url;
}

/**
 * Locates the most ideal file type, if it is available for the specified ref.
 * If you don't specify an ideal file type, webp or avif are assumed.
 * @param ref
 * @param wanted
 * @returns
 */
function idealType(ref: FileRefInfo, wanted : string | undefined) {
    var type = wanted || 'webp|avif';

    if (type != 'original' && ref.variants.length) {
        var wantedTypes = type.toLowerCase().split('|');
        var types = ref.typeMap();

        // If an ideal type is available, we use that instead.
        for (var i = 0; i < wantedTypes.length; i++) {
            var wantedType = wantedTypes[i];

            if (ref.fileType == wantedType || types[wantedType]) {
                return wantedType;
            }
        }
    }

    return ref.fileType;
}

/**
 * A file handler for uploaded (content) files.
 * @param refInfo The ref to build the URL of.
 * @param options Extra options for the URL, such as the file size if specifiable.
 * @returns
 */
function contentFile(refInfo: FileRefInfo, options: FileRefOptions) {
    var isPublic = refInfo.scheme == 'public';
    var url = isPublic ? '/content/' : '/content-private/';
    var dirs = refInfo.dirs;
    if (options.additionalDirectories) {
        dirs = dirs.concat(options.additionalDirectories);
    }

    var hadServer = false;

    if (dirs.length > 0) {
        url += dirs.join('/') + '/';
    }

    if (!hadServer && window.contentSource) {
        url = window.contentSource + url;
    }

    var name = refInfo.fileName;
    var type = isPublic ? idealType(refInfo, options.ideal) : refInfo.fileType;

    if (options.forceImage && isPublic) {
        if (!type || imageTypes.indexOf(type) == -1) {
            // Use the transcoded webp ver:
            type = 'webp';
        }
    }

    url = url + name + '-';

    if (options.size && options.size.indexOf && options.size.indexOf('.') != -1) {
        url += options.size;
    } else {
        url += (options.size || 'original') + (options.sizeExt || '') + '.' + type;
    }

    if (!isPublic) {
        // timestamp and sig
        url += '?t=' + refInfo.getArg('t', '') + '&s=' + refInfo.getArg('s', '');
    }

    return url;
}

var protocolHandlers : Record<string, FileRefHandler> = {
    'public': contentFile,
    'private': contentFile,
    's': staticFile,
    'url': basicUrl,
    'http': basicUrl,
    'https': basicUrl
};

/**
 * A textual or parsed file ref.
 */
export type FileRefIsh = FileRef | FileRefInfo;

/**
 * A protocol handler for a file ref. Returns the URL.
 */
type FileRefHandler = (r: FileRefInfo, opts: FileRefOptions) => string;

/**
 * A parsed FileRef.
 */
export class FileRefInfo {

    /**
     * The original source fileRef.
     */
    src: FileRef;

    /**
     * Usually "public". The scheme of the fileRef.
     */
    scheme: string;

    /**
     * Directories of the ref, if there are any.
     */
    dirs: string[];

    /**
     * The full file, including type, if there is one.
     */
    file?: string;
    
    /**
     * The filename (without any types), if there is one.
     */
    fileName?: string;

    /**
     * The main filetype if one is present.
     */
    fileType?: string;

    /**
     * Other filetypes if any are available. Usually transcoded variants.
     */
    variants: string[];

    /**
     * The truncated ref without the scheme.
     */
    ref: string;

    /**
     * The ref without the scheme and a query string if one is present.
     */
    basepath: string;

    /**
     * The full query string if one is present.
     */
    query: string;

    /**
     * The args parsed from the query string.
     */
    args: URLSearchParams;

    /**
     * Image author name if present.
     */
    author: string;

    /**
     * Image alt text if present.
     */
    altText: string;

    /**
     * X focal point.
     */
    focalX: number;

    /**
     * Y focal point.
     */
    focalY: number;

    /**
     * The handler for this fileRef.
     */
    handler: FileRefHandler;

    /**
     * Parses the general info from a FileRef.
     * @param src
     */
    constructor(src: FileRef) {
        this.src = src;
        var protoIndex = src.indexOf(':');
        var scheme = (protoIndex == -1) ? 'https' : src.substring(0, protoIndex);
        var ref = protoIndex == -1 ? src : src.substring(protoIndex + 1);
        var basepath = ref;

        var argsIndex = basepath.indexOf('?');
        var queryStr = '';
        if (argsIndex != -1) {
            queryStr = basepath.substring(argsIndex + 1);
            basepath = basepath.substring(0, argsIndex);
        }

        var handler = protocolHandlers[scheme];

        var fileParts : string[] | undefined = undefined;
        var fileType: string | undefined = undefined;
        var fileName: string | undefined = undefined;

        var dirs = basepath.split('/');
        var file = dirs.pop();
        var variants: string[] = [];

        if (file?.indexOf('.') != -1) {
            // It has a filetype - might have variants of the type too.
            fileParts = file?.split('.');
            fileName = fileParts?.shift();
            fileType = fileParts?.join('.');

            var multiTypes = basepath.indexOf('|');
            if (multiTypes != -1) {
                // Remove multi types from basepath:
                basepath = basepath.substring(0, multiTypes);

                // Get original (first) filetype and set that to fileType:
                var types = fileType?.split('|');
                fileType = types?.shift();

                if (types) {
                    variants = types;
                }

                // update file to also remove the | from it:
                file = fileName + '.' + fileType;
            }
        }

        var args = new URLSearchParams(queryStr);

        this.scheme = scheme;
        this.dirs = dirs;
        this.file = file;
        this.fileName = fileName;
        this.handler = handler;
        this.ref = ref;
        this.basepath = basepath;
        this.fileType = fileType;
        this.variants = variants;
        this.query = queryStr;
        this.args = args;
        this.focalX = this.getNumericArg('fx', 50);
        this.focalY = this.getNumericArg('fy', 50);
        this.altText = this.getArg('al', '');
        this.author = this.getArg('au', '');   
    }

    /**
     * Gets an arg by name or returns the specified default value if not found.
     * Parses the result as a number.
     * @param name
     * @param defaultVal
     */
    getNumericArg(name: string, defaultVal: number): number {
        var arg = this.args.get(name);
        return (arg === null) ? defaultVal : parseFloat(arg);
    }

    /**
     * Gets an arg by name or returns the specified default value if not found.
     * @param name
     * @param defaultVal
     */
    getArg(name:string, defaultVal:string) : string {
        var arg = this.args.get(name);
        return (arg === null) ? defaultVal : arg;
    }

    /**
     * Set a textual arg value.
     * @param arg
     * @param val
     */
    setArg(arg: string, val: string) {
        this.args.set(arg, val);
    }

    /**
     * Set a numeric arg value.
     * @param arg
     * @param val
     */
    setNumericArg(arg: string, val: number) {
        this.setArg(arg, val.toString());
    }

    /**
     * Gets a map of all available filetypes from this ref.
     * @returns
     */
    typeMap() {
        var map: Record<string, boolean> = {};
        if (this.fileType) {
            map[this.fileType.toLowerCase()] = true;
        }
        this.variants.forEach(variant => {
            map[variant.toLowerCase()] = true;
        });
        return map;
    }

    /**
     * True if this value is a content ref.
     * @returns
     */
    isRef() {
        if (this.handler !== undefined) {
            return this.handler.name == "contentFile";
        }

        return false;
    }


    /**
     * True if this ref is an image.
     * @param webOnly True if it should only check web compat image filetypes.
     * @returns
     */
	isImage(webOnly?: boolean) {
		if (this.fileType) {
			return ((webOnly ? MEDIA_TYPES.image.web : MEDIA_TYPES.image.all).indexOf(this.fileType) != -1);
        }

        return false;
    }

    /**
     * True if this ref is a video.
     * @param webOnly True if it should only check web compat video filetypes.
     * @returns
     */
    isVideo(webOnly?: boolean) {
        if (this.fileType) {
			return ((webOnly ? MEDIA_TYPES.video.web : MEDIA_TYPES.video.all).indexOf(this.fileType) != -1);
        }

        return false;
    }

	/**
	 * True if this ref contains audio.
	 * @param webOnly True if it should only check web compat audio filetypes.
	 * @returns
	 */
	isAudio(webOnly?: boolean) {
		if (this.fileType) {
			return ((webOnly ? MEDIA_TYPES.audio.web : MEDIA_TYPES.audio.all).indexOf(this.fileType) != -1);
		}

		return false;
	}

	/**
	 * True if this ref is a document.
	 * @param webOnly True if it should only check web compat document filetypes.
	 * @returns
	 */
	isDocument(webOnly?: boolean) {
		if (this.fileType) {
			return ((webOnly ? MEDIA_TYPES.document.web : MEDIA_TYPES.document.all).indexOf(this.fileType) != -1);
		}

		return false;
	}

    /**
     * The original unmodified ref.
     * Different from toString() which constructs a new string.
     * @returns
     */
    getSource() {
        return this.src;
    }

    /**
     * Builds the ref string from the fields.
     */
    toString() {
        var result = this.scheme + ":";
        result += this.basepath;
        if (this.variants && this.variants.length) {
            this.variants.forEach(v => result += '|' + v);
        }
        var q = this.args.toString();
        if (q) {
            result += '?' + q;
        }
        return result;
    }
	
	/**
	* Gets the width of the original media, if known.
	*/
	getWidth() {
		var w = this.args.get('w');
		if(!w){
			return undefined;
		}
		
		var wInt = parseInt(w);
		
		if(isNaN(wInt)){
			return undefined;
		}
		
		return wInt;
	}
	
	/**
	* Gets the height of the original media, if known.
	*/
	getHeight() {
		var h = this.args.get('h');
		if(!h){
			return undefined;
		}
		
		var hInt = parseInt(h);
		
		if(isNaN(hInt)){
			return undefined;
		}
		
		return hInt;
	}
	
	/**
	* Undefined if it is indeterminate (e.g. it does not have a w/h arg).
	*/
	isPortrait() {
		var w = this.getWidth();
		var h = this.getHeight();
		
		if(w === undefined || h === undefined){
			return undefined;
		}
		
		return w < h;
	}
}

/**
 * Parses a textual fileRef to a FileRefInfo object.
 * If you give it a FileRefInfo it will be returned as-is.
 * @param ref The fileRef.
 * @returns
 */
export function parse (ref: FileRefIsh): FileRefInfo | null {
    if (!ref || ref == "") {
        return null;
    }
    if ((ref as FileRefInfo).scheme) {
        return ref as FileRefInfo;
    }
    return new FileRefInfo(ref as FileRef);
};

/*
* Convenience method for identifying refs.
*/
export function isRef(ref : FileRefIsh) {
    var info = parse(ref);
    return info ? info.isRef() : false;
}

/*
* Convenience method for identifying image refs.
*/
export function isImage(ref: FileRefIsh, webOnly?: boolean) {
    var info = parse(ref);
    return info ? info.isImage(webOnly) : false;
}

/**
 * Convenience method for identifying videos.
 * @param ref
 * @param webOnly True if it should only check for web compatible videos.
 * @returns
 */
export function isVideo(ref: FileRefIsh, webOnly?: boolean) {
    var info = parse(ref);
    return info ? info.isVideo(webOnly) : false;
}

/**
 * Convenience method for identifying audio.
 * @param ref
 * @param webOnly True if it should only check for web compatible audio.
 * @returns
 */
export function isAudio(ref: FileRefIsh, webOnly?: boolean) {
	var info = parse(ref);
	return info ? info.isAudio(webOnly) : false;
}

/**
* Convenience method for identifying documents.
* @param ref
* @param webOnly True if it should only check for web compatible documents.
* @returns
*/
export function isDocument(ref: FileRefIsh, webOnly?: boolean) {
	var info = parse(ref);
	return info ? info.isDocument(webOnly) : false;
}

/**
* Convenience method for identifying other file types (namely not images, video, audio or documents - e.g. ZIP).
* @param ref
* @param webOnly True if it should only check for web compatible file types.
* @returns
*/
export function isOther(ref: FileRefIsh, webOnly?: boolean) {
	var info = parse(ref);

	return info ? !info.isImage(webOnly) && !info.isVideo(webOnly) && !info.isAudio(webOnly) && !info.isDocument(webOnly) : false;
}