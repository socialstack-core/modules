import Image from 'UI/Image';
import Link from 'UI/Link';
import * as fileRef from 'UI/FileRef';

export type InlinePromotionConditions = {
	pages?: int[];
	searchCategories?: int[];
	categories?: int[];
	includeChildren?: boolean;
	products?: int[];
	minPrice?: int | null;
	maxPrice?: int | null;
};

export type InlinePromotion = {
	id: number;
	name: string;
	isActive: boolean;
	startDate?: string;
	endDate?: string;
	cta?: string;
	description?: string;
	targetUrl?: string;
	featureRef?: string;
	stickerTitle?: string;
	stickerDescription?: string;
	conditions?: InlinePromotionConditions;
	placementType?: string;
}

/**
 * Props for the Promotion component.
 */
interface PromotionProps {
	/**
	 * optional background image
	 */
	featureRef?: FileRef,

	/**
	 * optional promotion image (square aspect ratio with transparent background recommended)
	 */
	promoRef?: FileRef,

	/**
	 * optionally nudge promotion image horizontally
	 */
	promoOffsetX?: number,

	/**
	 * optionally nudge promotion image vertically
	 */
	promoOffsetY?: number,

	/**
	 * render promotion image clipped / offset (default is to display full image)
	 */
	promoClipped?: boolean,

	/**
	 * set true to switch all white pixels within promo image to transparency
	 */
	removeWhiteBackground?: boolean,

	/**
	 * promotion title (optional if promotion prop is provided)
	 */
	title?: string,

	/**
	 * promotion description (optional if promotion prop is provided)
	 */
	description?: string,

	/**
	 * call to action text (defaults to "Shop now")
	 */
	cta?: string,

	/**
	 * call to action link (optional if promotion prop is provided)
	 */
	url?: string,

	/**
	 * optional sticker title
	 */
	stickerTitle?: string,

	/**
	 * optional sticker description
	 */
	stickerDescription?: string,

	/**
	 * optional classnames
	 */
	className?: string,

	/**
	 * Optional object prop from BodyJson (promotions array)
	 */
	promotion?: InlinePromotion;
}

/**
 * The Promotion React component.
 * @param props React props.
 */
const Promotion: React.FC<PromotionProps> = (props) => {
	const inlinePromo = props.promotion;
	
	// Use BodyJson data if provided, otherwise use individual props
	const backgroundRef = undefined;
	const promoRef = inlinePromo ? inlinePromo.featureRef : props.featureRef;
	const promoOffsetX = inlinePromo ? 0 : props.promoOffsetX;
	const promoOffsetY = inlinePromo ? 0 : props.promoOffsetY;
	const removeWhiteBackground = props.removeWhiteBackground;

	const title = inlinePromo?.name || props.title;
	const description = inlinePromo?.description || props.description;
	const cta = inlinePromo?.cta || `Shop now`;
	const url = inlinePromo?.targetUrl || props.url;

	// If no title or URL, don't render
	if (!title || !url) {
		return null;
	}

	const className = props.className;
	const stickerTitle = inlinePromo ? (inlinePromo as any).stickerTitle : props.stickerTitle;
	const stickerDescription = inlinePromo ? (inlinePromo as any).stickerDescription : props.stickerDescription;

	var baseClass = 'ui-promotion';
	var classNames = [baseClass];

	if (removeWhiteBackground) {
		classNames.push(`${baseClass}--white-to-transparent`);
	}

	if (props.promoClipped) {
		classNames.push(`${baseClass}--clipped`);
	}

	if (className?.trim().length) {
		classNames.push(className);
	}

	const hasSticker = !!(stickerTitle || stickerDescription);

	const backgroundStyle = backgroundRef ? {
		backgroundImage: `url(${fileRef.getUrl(backgroundRef)})`
	} : {};

	const offsetX = promoOffsetX || 0;
	const offsetY = promoOffsetY || 0;

	const promoStyle = offsetX == 0 && offsetY == 0 ? {} : {
		transform: `translate(${offsetX}px, ${offsetY}px)`
	};

	return (
		<div className={classNames.join(' ')} style={backgroundStyle}>
			<div className={`${baseClass}__internal`}>
				{promoRef && <>
					<Image className={`${baseClass}__image`} size={512} fileRef={promoRef} style={promoStyle} />
				</>}
				<h4 className={`${baseClass}__title`}>
					{title}
				</h4>
				<p className={`${baseClass}__description`}>
					{description}
				</p>
				<Link href={url} className={`${baseClass}__cta`}>
					<span>
						{cta || `Shop now`}
					</span>
					<i className="fr fr-arrow-right"></i>
				</Link>

				{hasSticker && (
					<div className={`${baseClass}__sticker`}>
						<div className={`${baseClass}__sticker-bg`}></div>
						<div className={`${baseClass}__sticker-peel`}></div>
						{stickerTitle && <p>{stickerTitle}</p>}
						{stickerDescription && <small>{stickerDescription}</small>}
					</div>
				)}
				
			</div>
		</div>
	);
}

export default Promotion;