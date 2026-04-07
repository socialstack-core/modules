import Link from 'UI/Link';
import { getUrl, parse, FileRefInfo } from 'UI/FileRef';
import { decode, drawImageDataOnNewCanvas } from 'UI/Functions/Blurhash';
import getConfig from 'UI/Config';

const BLURHASH_WIDTH = 64;
const BLURHASH_HEIGHT = 36;
const MIN_SRCSET_WIDTH = 256;
export type ImageSize      = "original" | number | string; // add more as necessary
export type ImageAlignment = "none" | "left" | "right" | "center";

/**
 * Props for the image component.
 * @icon fal fa-image
 * @description Displays an image.
 */
export interface ImageProps {
    onClick?: React.MouseEventHandler<HTMLImageElement>, 
	fileRef: FileRef, 
	/**
	 * If provided, this image displays if the 
	 * device is portrait and the base fileRef if it is landscape.
	 */
    portraitRef?: FileRef, 
    linkUrl?: string, 
    size?: ImageSize, 
    responsive?: boolean, // (default is true)
    blurhash?: boolean, // (default is true)
    float?: ImageAlignment, 
    className?: string,
	animation?: string, 
    animationDirection?: string, 
	animationDuration?: number,
	/**
	 * Constrains the Y axis to your specified size if an image is actually portrait.
	 */
	autoPortrait?: boolean,
	width?: number, 
    height?: number, 
	title?: string,
	style?: React.CSSProperties,
	asFigure?: boolean,
	hideOnError?: boolean,
	lazyLoad?: boolean,
	fetchPriority?: "high" | "low" | "auto"

	/**
	 * disable <picture> wrapper, output plain <img /> tag only
	 */
	plain?: boolean,
}

let _sortedSizes: number[] | undefined;

/**
 * Supported sizes from largest to smallest.
 * @returns
 */
function getSupportedSizes() {
	if (_sortedSizes) {
		return _sortedSizes;
	}
	var cfg = getConfig<any>('uploader');
	var origSizes = cfg?.[0]?.imageSizes ?? null;

	var sizes = origSizes ? [...origSizes] : [];
	return sizes.sort((a, b) => b - a);
}

type ImageSource = {
	size: number,
	url: string,
	format: string,
	mediaQuery: string
}

/**
 * Gets all possible sources for a given ref.
 * @param ref
 * @returns
 */
function getAllSources(ref: FileRefInfo, maxSize?: ImageSize, mediaQuery?: string) : ImageSource[] {
	var supportedSizes = getSupportedSizes();
	var sources: ImageSource[] = [];

	let maxSizeN = 0;

	if (typeof maxSize == 'number') {
		maxSizeN = maxSize;
	}

	// The order of variants matters: the first variant is deemed the most favoured.
	var variants = [...ref.variants, 'original'];

	variants.forEach(variant => {

		supportedSizes?.forEach((size, sizeIndex) => {
			if (maxSizeN && size > maxSizeN) {
				return;
			}

			const url = getUrl(ref, { size: size.toString(), ideal: variant });
			if (url && ref.fileType) {
				var prevSize = sizeIndex == supportedSizes.length - 1 ? 0 : (supportedSizes[sizeIndex + 1] + 1);
				var mq = '(min-width: ' + prevSize + 'px)';

				sources.push({
					size,
					url,
					mediaQuery: mediaQuery ? mq + ' and ' + mediaQuery : mq,
					format: getMimeType(
						variant == 'original' ? ref.fileType.toLowerCase() : variant
					)
				});
			}
		});
	});

	return sources;
}

/**
 * Image mime types.
 * @param extension
 * @returns
 */
function getMimeType(extension: string) {
	if (extension == 'svg') {
		return 'text/svg';
	}

	return 'image/' + extension;
}

function toPictureSources(sources: ImageSource[]) {
	return sources.map((src, index) => {
		return <source media={src.mediaQuery} type={src.format} srcSet={src.url} />;
	});
}

const Image: React.FC<ImageProps> = (props: ImageProps): React.ReactNode => {
    
    const {
        fileRef,
        width, 
        height,
		blurhash,
		size,
		style,
		title,
		asFigure,
		hideOnError,
		lazyLoad,
		fetchPriority,
		autoPortrait,
		portraitRef,
		plain
    } = props;
	
	const responsive = props.responsive === undefined ? true : props.responsive;
	
    // join class names as opposed to += 
    const classNames: string[] = [props.className ? `ui-image__wrapper ${props.className}` : 'ui-image__wrapper'];
    let animation: string | null = null; 

    if (!fileRef) {
		return (<div style={{width, height, backgroundColor: 'grey', color: 'white', textAlign: 'center', display: 'inline-block'}}>
			<div style={{margin: '10px'}}>
				<i className='fa fa-camera' />
			</div>
			{`No source`}
		</div>);
    }
	
    // adds an alignment CSS class to the image
    if (props.float && props.float !== "none") {
        classNames.push(`ui-image--${props.float}`);
    }

    // any animation functionality.
    if (props.animation) {
        animation = props.animation;
        animation += "-" + (props.animationDirection ?? "up");
    }

    if (!responsive || size === "original" && width && !isNaN(width)) {
        classNames.push("ui-image--wide");
    }
	
	var ref = parse(fileRef);

	if (ref == null) {
		return;
	}

	var isPortrait = autoPortrait ? ref.isPortrait() : false;

	if (isPortrait) {
		// Constrain picture *height* to that of the requested size.
	}

	var authorText = ref.getArg('author', '');

	var altText = title;
	if (!altText) {
		altText = ref.getArg('alt', '');
	}

	// Get all of the available sources:
	var sources = getAllSources(ref, size);

	if (portraitRef) {
		var portraitInfo = parse(portraitRef);

		if (portraitInfo) {
			var portraitSources = getAllSources(portraitInfo, size, '(orientation: portrait)');

			// Portrait set is higher priority and thus lists first:
			sources = [...portraitSources, ...sources];
		}
	}

	/*
	This would go in a useEffect.
	if (r.fileType && r.fileType.toLowerCase() != 'svg') {
	
		// blurhash check
		if (r.args && r.args.b) {
			var blurhash = r.args.b;
			var blurhash_width = Math.min(r.args.w || BLURHASH_WIDTH, BLURHASH_WIDTH);
			var blurhash_height = Math.min(r.args.h || BLURHASH_HEIGHT, BLURHASH_HEIGHT);
			
			var imgData = decode(blurhash, blurhash_width, blurhash_height);
			var canvas = drawImageDataOnNewCanvas(imgData, blurhash_width, blurhash_height);
			var backgroundImage = `url(${canvas.toDataURL()})`;
		}
	}
	*/
	
    var imgUrl = getUrl(ref, {size: size?.toString()});

	var altWithAuthor: string | undefined = altText;
	if (authorText) {
		if (altWithAuthor) {
			altWithAuthor += ', ';
		}

		altWithAuthor += 'Author: ' + altText;
	}

	if (!altWithAuthor?.length) {
		altWithAuthor = undefined;
	}

	var fx = ref.getNumericArg('fx', 50);
	var fy = ref.getNumericArg('fy', 50);

	if (plain) {
		var plainClasses = ['ui-image'];

		if (props.className) {
			plainClasses.push(props.className);
		}

		return <>
			<img
				className={plainClasses.join(' ')}
				src={imgUrl}
				alt={altWithAuthor}
				width={width}
				height={height}
				fetchPriority={fetchPriority}
				loading={lazyLoad === false ? undefined : "lazy"}
				onError={hideOnError ? (e) => {
					e.currentTarget.style.display = 'none';
				} : undefined}
				style={(fx != 50 || fy != 50) ? { objectPosition: `${fx}% ${fy}%` } : undefined}
			/>
		</>;
	}

	var picContent = <picture 
		className={classNames.join(" ")}
		style={style}
		onClick={props.onClick}
	>
		{toPictureSources(sources)}
		<img
		    className="ui-image"
			src={imgUrl}
			alt={altWithAuthor}
			width={width}
			height={height}
			fetchPriority={fetchPriority}
			loading={lazyLoad === false ? undefined : "lazy"}
			onError={hideOnError ? (e) => {
				e.currentTarget.style.display = 'none';
			} : undefined}
			style={(fx != 50 || fy != 50) ? { objectPosition: `${fx}% ${fy}%` } : undefined}
		/>
	</picture>;

	if (asFigure) {
		// If you'd like semantics, always mark it as a figure.
		picContent = <FigureWrapper caption={altText} author={authorText}>
			{picContent}
		</FigureWrapper>;
	}

    if (props.linkUrl) {
		picContent = <Link
			href={props.linkUrl}
		>
			{picContent}
		</Link>;
    }
	
	return picContent;
}

type FigureWrapperProps = {
	caption?: string,
	author?: string,
};

const FigureWrapper: React.FC<React.PropsWithChildren<FigureWrapperProps>> = (props): React.ReactNode => {
	const { children, caption, author } = props;

	return <figure itemScope itemType="https://schema.org/ImageObject">
		{children}
		{caption && <figcaption className="responsive-media__caption">
			{caption}
			{author &&
				<>
					<span itemProp="creator" itemType="https://schema.org/Person" itemScope>
						<meta itemProp="name" content={author} />
					</span>
					<p itemProp="copyrightNotice">© {new Date().getFullYear()} - <span itemProp="creditText">{author}</span></p>
				</>
			}
		</figcaption>}
	</figure>;
}

export default Image;