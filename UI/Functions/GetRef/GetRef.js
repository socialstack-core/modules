import { decode, drawImageDataOnNewCanvas } from 'UI/Functions/Blurhash';
import { useConfig } from 'UI/Session';

/*
* Content refs are handled by the frontend because they can also have custom handlers.
* for example, youtube:8hWsalYQhWk or fa:fa-user are valid refs too.
* This method converts a textual ref to either a HTML representation or a URL, depending on if you have options.url set to true.
* Specific ref protocols (like public: private: fa: youtube: etc) use the options as they wish.
*/

const HIDE_ON_ERROR = (e) => {
    e.currentTarget.style.display = 'none';
};
const BLURHASH_WIDTH = 64;
const BLURHASH_HEIGHT = 36;
const MIN_SRCSET_WIDTH = 256;

export default function getRef(ref, options) {
    var r = getRef.parse(ref);
    var options = options || {};
    var isImage = getRef.isImage(ref);
    var isIcon = getRef.isIcon(ref);

    // portrait image (or video) check
    // (used within handler to determine which of height/width attributes are set)
    if (options.portraitCheck && isImage && !isIcon &&
        r.args && r.args.w && r.args.h && r.args.w < r.args.h) {
        options.isPortrait = true;
    }

    // image handling
    if (isImage && !isIcon && r.fileType && r.fileType.toLowerCase() != 'svg') {
        options.responsiveSizes = getSizes(options, r);
        options.landscapeSrcset = getSrcset(options, r);

        if (options.portraitRef) {
            var rp = getRef.parse(options.portraitRef);
            options.portraitSrcset = getSrcset(options.portraitRef, options, rp);
        }

        // focal point check
        if (!(r.focalX == 50 && r.focalY == 50)) {
            options.fx = r.focalX;
            options.fy = r.focalY;
        }

        // blurhash check
        if (r.args && r.args.b) {
            options.blurhash = r.args.b;
            options.blurhash_width = Math.min(r.args.w || BLURHASH_WIDTH, BLURHASH_WIDTH);
            options.blurhash_height = Math.min(r.args.h || BLURHASH_HEIGHT, BLURHASH_HEIGHT);
        }

        // author
        if (r.author.length > 0) {
            options.author = r.author;
        }

        // alternate name
        if (r.altText.length > 0) {
            options.altText = r.altText;
        }
    }

    return r ? r.handler(r.basepath, options, r) : null;
}

function getSupportedSizes() {
    var uploaderConfig = useConfig('uploader') || {};
    var sizes = uploaderConfig.imageSizes || [32, 64, 100, 128, 200, 256, 512, 768, 1024, 1920, 2048];
    return sizes.filter(size => size >= 256);
}

function getSrcset(options, r) {

    if (!r) {
        return '';
    }

    var supportedSizes = getSupportedSizes();
    var width = options.size || ((r.args && r.args.w) ? r.args.w : undefined);

    if (!width) {
        width = supportedSizes[supportedSizes.length - 1];
    }

    var srcset = [];

    supportedSizes.forEach(size => {

        if (size >= MIN_SRCSET_WIDTH && size <= width) {
            var url = r.handler(r.basepath, { size: size, url: true }, r);
            srcset.push(`${url} ${size}w`);
        }

    });

    return srcset.length < 2 ? undefined : srcset.join(',');
}

function getSizes(options, r) {
    var supportedSizes = getSupportedSizes();
    var width = options.size || ((r.args && r.args.w) ? r.args.w : undefined);

    if (!width) {
        width = supportedSizes[supportedSizes.length - 1];
    }

    var sizes = [];

    supportedSizes.forEach((size, i) => {

        if (size >= MIN_SRCSET_WIDTH && size <= width) {
            sizes.push(i == supportedSizes.length - 1 ?
                `${size}px` :
                `(max-width: ${size}px) ${size}px`);
        }

    });

    return sizes.length < 2 ? undefined : sizes.join(',');
}

function basicUrl(url, options, r) {
    // strip out any leading slashes from url
    var qualifiedUrl = r.scheme + '://' + url.replace(/^\/+/, '');;

    if (options.size) {
        let fileIndex = qualifiedUrl.lastIndexOf(r.file);

        if (fileIndex != -1 && r.fileName.endsWith("-original")) {
            qualifiedUrl = `${qualifiedUrl.slice(0, fileIndex)}${r.fileName.replace('-original', '-' + options.size)}.${r.fileType}`;
        }
    }

    if (options.url) {
        return qualifiedUrl;
    }

    return displayImage(qualifiedUrl, options);
}

function staticFile(basepath, options, r) {
    var refParts = basepath.split('/');
    var mainDir = refParts.shift();
    var cfg = global.config;
    var url = (cfg && cfg.pageRouter && cfg.pageRouter.hash ? 'pack/static/' : '/pack/static/') + refParts.join('/');
    if (mainDir.toLowerCase() == 'admin') {
        url = '/en-admin' + url;
    }

    url = (global.staticContentSource || '') + url;

    if (options.size) {
        let fileIndex = url.lastIndexOf(r.file);

        if (fileIndex != -1 && r.fileName.endsWith("-original")) {
            url = `${url.slice(0, fileIndex)}${r.fileName.replace('-original', '-' + options.size)}.${r.fileType}`;
        }
    }

    if (options.url) {
        return url;
    }

    return displayImage(url, options);
}

function idealType(ref, wanted) {
    wanted = wanted || 'webp|avif';

    if (wanted != 'original' && ref.variants.length) {
        var wantedTypes = wanted.toLowerCase().split('|');
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

function contentFile(basepath, options, r) {
    var isPublic = r.scheme == 'public';
    var url = isPublic ? '/content/' : '/content-private/';
    var dirs = r.dirs;
    if (options.dirs) {
        dirs = dirs.concat(options.dirs);
    }

    var hadServer = false;

    if (dirs.length > 0) {
        // If dirs[0] contains . then it's a server address (for example, public:mycdn.com/123.jpg
        if (dirs[0].indexOf('.') != -1) {
            var addr = dirs.shift();
            url = '//' + addr + url;
            hadServer = true;
        }

        url += dirs.join('/') + '/';
    }

    if (!hadServer && global.contentSource) {
        url = global.contentSource + url;
    }

    var name = r.fileName;
    var type = isPublic ? idealType(r, options.ideal) : r.fileType;

    if (options.forceImage && isPublic) {
        if (imgTypes.indexOf(type) == -1) {
            // Use the transcoded webp ver:
            type = 'webp';
        }
    }

    // Web video only:
    var video = (type == 'mp4' || type == 'webm' || type == 'avif');

    url = url + name + '-';

    if (options.size && options.size.indexOf && options.size.indexOf('.') != -1) {
        url += options.size;
    } else {
        url += ((video || type == 'svg' || type == 'apng' || type == 'gif') ? (options.videoSize || 'original') : (options.size || 'original')) + (options.sizeExt || '') + '.' + type;
    }

    if (!isPublic) {
        url += '?t=' + r.args.t + '&s=' + r.args.s;
    }

    if (options.url) {
        return url;
    }

    if (video) {
        var videoSize = options.size || 256;
        return <>
            <video className="responsive-media__video" src={url}
                width={options.isPortrait ? undefined : videoSize} height={options.isPortrait ? videoSize : undefined}
                loading={options.lazyLoad == false ? undefined : "lazy"} controls {...options.attribs} />
        </>;
    }

    return displayImage(url, options);
}

function displayImage(url, options) {
    var imgSize = options.size || undefined;
    var renderedWidth = imgSize || (options.args && options.args.w ? options.args.w : undefined);
    var ImgWrapperTag = options.portraitRef ? 'picture' : 'div';
    var hasWrapper = options.forceWrapper || false;
    var wrapperStyle = {};

    if (options.fx && options.fy && !(options.fx == 50 && options.fy == 50)) {
        wrapperStyle.backgroundPosition = `${options.fx}% ${options.fy}%`;
    }

    // check - blurhash available?
    if (options.blurhash && !window.SERVER) {
        var imgData = decode(options.blurhash, options.blurhash_width, options.blurhash_height);
        var canvas = drawImageDataOnNewCanvas(imgData, options.blurhash_width, options.blurhash_height);
        wrapperStyle.backgroundImage = `url(${canvas.toDataURL()})`;
        hasWrapper = true;
    }

    // if we need to support art direction (e.g. portrait / landscape versions of the same image)
    if (options.portraitRef) {
        hasWrapper = true;
    }

    var width = options.isPortrait ? undefined : renderedWidth;
    var height = options.isPortrait ? renderedWidth : undefined;

    if (options.forceWidth) {
        width = options.forceWidth;
    }

    if (options.forceHeight) {
        height = options.forceHeight;
    }

    if (width) {
        wrapperStyle.width = `${width}px`;
    }

    if (height) {
        wrapperStyle.height = `${height}px`;
    }

    if (options.figure) {
        ImgWrapperTag = 'figure';
        hasWrapper = true;
    }

    var imgWidth = hasWrapper ? undefined : width;
    var imgHeight = hasWrapper ? undefined : height;

    var authorText = (options.author && options.author.length > 0) ? options.author : '';

    var altText = options.alt;
    if (altText == undefined || altText.length == 0) {
        if (options.attrib && options.attribs.alt != undefined && options.attribs.alt.length > 0) {
            altText = options.attribs.alt;
        } else if (options.altText != undefined && options.altText.length > 0) {
            altText = options.altText;
            if (authorText.length > 0) {
                altText += ` by ` + authorText;
            }
        }
    }

    if (options.forceWidth) {
        imgWidth = width;
    }

    if (options.forceHeight) {
        imgHeight = height;
    }

    if ((options.portraitRef && options.portraitSrcset && options.responsiveSizes) || (options.landscapeSrcset && options.responsiveSizes)) {
        ImgWrapperTag = "picture";
    }

    // setup structured data for the image based on the author
    var structuredDataHeader = (authorText && authorText.length > 0) ? { "itemscope": "", itemtype: "https://schema.org/ImageObject" } : {};
    var structuredDataItem = (authorText && authorText.length > 0) ? { itemprop:"contentUrl" } : {};

    var img = <img className="responsive-media__image" src={url} srcset={hasWrapper ? undefined : options.landscapeSrcset}
        style={options.fx && options.fy && !(options.fx == 50 && options.fy == 50) ? { 'object-position': `${options.fx}% ${options.fy}%` } : undefined}
        width={imgWidth}
        height={imgHeight}
        fetchPriority={options.fetchPriority}
        loading={options.lazyLoad == false ? undefined : "lazy"}
        {...options.attribs}
        alt={altText}
        onerror={options.hideOnError ? HIDE_ON_ERROR : undefined}
        {...structuredDataItem}
    />;

    if (hasWrapper) {
        return (
            // support art direction / blurhash background
            <ImgWrapperTag {...structuredDataHeader} className="responsive-media__wrapper" style={wrapperStyle}>
                {options.portraitRef && options.portraitSrcset && options.responsiveSizes && <>
                    <source
                        media="(orientation: portrait)"
                        srcset={options.portraitSrcset}
                        sizes={options.responsiveSizes}
                    />
                </>}
                {options.landscapeSrcset && options.responsiveSizes && <>
                    <source
                        media={options.portraitRef ? "(orientation: landscape)" : undefined}
                        srcset={options.landscapeSrcset}
                        sizes={options.responsiveSizes}
                    />
                </>}
                {img}
                {options.figure && options.figCaption && <figcaption className="responsive-media__caption">
                    {options.figCaption}
                    {structuredDataHeader && structuredDataHeader.itemtype &&
                    <>
                        <span itemprop="creator" itemtype="https://schema.org/Person" itemscope>
                            <meta itemprop="name" content={authorText} />
                        </span>
                        {/* <span style='display:none;' itemprop="copyrightNotice">{authorText}</span>
                        <span style='display:none;' itemprop="creditText">{authorText}</span> */}
                        <p itemprop="copyrightNotice">© {new Date().getFullYear()} - <span itemprop="creditText">{authorText}</span></p>
                    </>
                }
                </figcaption>}
            </ImgWrapperTag>
        );
    }

    // plain image with author data
    if (structuredDataHeader && structuredDataHeader.itemtype)
    {
        return (
            <div {...structuredDataHeader}>
                {img}
                <span itemprop="creator" itemtype="https://schema.org/Person" itemscope>
                    <meta itemprop="name" content={authorText} />
                </span>
                <span style='display:none;' itemprop="copyrightNotice">{authorText}</span>
                <span style='display:none;' itemprop="creditText">{authorText}</span>
            </div>
        );
    }

    // basic image
    return img;
}

function fontAwesomeIcon(basepath, options, r) {
    // Note: doesn't support options.url (yet!)
    if (options.url) {
        return '';
    }

    // allows us to specify FontAwesome modifier classes, e.g. fa-fw
    var className = r.scheme + ' ' + basepath;

    if (options.className) {
        className += ' ' + options.className;
    }

    if (options.classNameOnly) {
        return className;
    }

    return (<i className={className} {...options.attribs} />);
}

function emojiStr(basepath, options) {
    // Note: doesn't support options.url (yet!)
    if (options.url) {
        return '';
    }
    var emojiString = String.fromCodePoint.apply(String, basepath.split(',').map(num => parseInt('0x' + num)));
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

function getArg(args, key, value) {
    if (args && args[key]) {
        return args[key]
    }

    return value;
}

function parseArgs(query) {
    var args = {};
    if (query) {
        var vars = query.split('&');
        for (var i = 0; i < vars.length; i++) {
            var pair = vars[i].split('=');
            var p1 = decodeURIComponent(pair[0]);
            var p2 = pair.length > 1 ? decodeURIComponent(pair[1]) : true;

            // check - numeric value?
            if (p1 != 't') {
                var parsedFloat = parseFloat(p2);
                args[p1] = isNaN(parsedFloat) ? p2 : parsedFloat;
            } else {
                args[p1] = p2;
            }
        }
    }
    return args;
}

function encodeArgs(args) {
    var query = '';
    if (args) {

        var delimiter = '?';

        Object.keys(args).forEach(function (key) {
            var item = key + "=" + encodeURIComponent(args[key]);

            query += (query.length == 0 ? '?' : '&') + item;
        });
    }
    return query;
}

getRef.update = (ref, args) => {
    if (!args) {
        return ref;
    }

    var info = getRef.parse(ref);

    if (!info) {
        return ref;
    }

    if (info.scheme == 'private') {
        return ref;
    }

    var basepath = ref;

    var argsIndex = basepath.indexOf('?');
    var queryStr = '';
    if (argsIndex != -1) {
        queryStr = basepath.substring(argsIndex + 1);
        basepath = basepath.substring(0, argsIndex);
    }

    var existingArgs = info.args;

    Object.keys(args).forEach(function (key) {
        existingArgs[key] = args[key];
    });

    return basepath + encodeArgs(existingArgs);
};

getRef.parse = (ref) => {
    if (!ref || ref == "") {
        return null;
    }
    if (ref.scheme) {
        return ref;
    }
    var src = ref;
    var protoIndex = ref.indexOf(':')
    var scheme = (protoIndex == -1) ? 'https' : ref.substring(0, protoIndex);
    ref = protoIndex == -1 ? ref : ref.substring(protoIndex + 1);
    var basepath = ref;

    var argsIndex = basepath.indexOf('?');
    var queryStr = '';
    if (argsIndex != -1) {
        queryStr = basepath.substring(argsIndex + 1);
        basepath = basepath.substring(0, argsIndex);
    }

    var handler = protocolHandlers[scheme];

    if (!handler) {
        return null;
    }

    var fileParts = null;
    var fileType = null;
    var fileName = null;

    var dirs = basepath.split('/');
    var file = dirs.pop();

    var variants = [];
    if (file.indexOf('.') != -1) {
        // It has a filetype - might have variants of the type too.
        fileParts = file.split('.');
        fileName = fileParts.shift();
        fileType = fileParts.join('.');

        var multiTypes = basepath.indexOf('|');
        if (multiTypes != -1) {
            // Remove multi types from basepath:
            basepath = basepath.substring(0, multiTypes);

            // Get original (first) filetype and set that to fileType:
            var types = fileType.split('|');
            fileType = types.shift();
            variants = types;

            // update file to also remove the | from it:
            file = fileName + '.' + fileType;
        }
    }
	
	var args = parseArgs(queryStr);
	
    var refInfo = {
        src,
        scheme,
        dirs,
        file,
        fileName,
        handler,
        ref,
        basepath,
        fileType,
        variants,
        query: queryStr,
        typeMap: () => {
            var map = {};
            if (fileType) {
                map[fileType.toLowerCase()] = 1;
            }
            variants.forEach(variant => {
                map[variant.toLowerCase()] = 1;
            });
            return map;
        },
        args,
        focalX: getArg(args, 'fx', 50),
        focalY: getArg(args, 'fy', 50),
        altText: getArg(args, 'al', ''),
        author: getArg(args, 'au', '')
    };

    refInfo.toString = () => {
        return refInfo.src;
    };

    return refInfo;
};

/*
* Convenience method for identifying visual refs (including videos).
*/
var imgTypes = ['png', 'jpeg', 'jpg', 'gif', 'mp4', 'svg', 'bmp', 'apng', 'avif', 'webp'];
var vidTypes = ['mp4', 'webm', 'avif'];
var allVidTypes = ['avi', 'wmv', 'ts', 'm3u8', 'ogv', 'flv', 'h264', 'h265', 'webm', 'ogg', 'mp4', 'mkv', 'mpeg', '3g2', '3gp', 'mov', 'media', 'avif'];
var allIconTypes = ['fa', 'fas', 'far', 'fad', 'fal', 'fab', 'fr'];

getRef.isImage = (ref) => {
    var info = getRef.parse(ref);
    if (!info) {
        return false;
    }

    if (info.scheme == 'private') {
        return false;
    } else if (info.scheme == 'url' || info.scheme == 'http' || info.scheme == 'https' || info.scheme == 'public') {
        return (imgTypes.indexOf(info.fileType) != -1);
    }

    // All other ref types are visual (fontawesome etc):
    return true;
}

getRef.isVideo = (ref, webOnly) => {
    var info = getRef.parse(ref);
    if (!info) {
        return false;
    }

    if (info.scheme == 'private') {
        return false;
    } else if (info.scheme == 'url' || info.scheme == 'http' || info.scheme == 'https' || info.scheme == 'public') {
        return ((webOnly ? vidTypes : allVidTypes).indexOf(info.fileType) != -1);
    }

    return false;
}

getRef.isIcon = (ref) => {
    var info = getRef.parse(ref);

    if (!info) {
        return false;
    }

    if (info.scheme == 'private') {
        return false;
    }

    return (allIconTypes.indexOf(info.scheme) != -1);
}
