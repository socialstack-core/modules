import * as fileRef from 'UI/FileRef';
import Html from 'UI/Html';
import Link from 'UI/Link';

/**
 * Props for the promotional banner component.
 */
interface BannerProps {
	/**
	 * background image reference
	 */
	backgroundImageRef: FileRef,

	/**
	 * title (optional)
	 */
	title?: string,

	/**
	 * description (optional)
	 */
	description?: HtmlString,

	/**
	 * label for optional CTA link
	 */
	ctaLabel?: string,

	/**
	 * URL for optional CTA link
	 */
	ctaLink?: string,

	/**
	 * optional additional classes
	 */
	className?: string
}

/**
 * The promotional banner React component.
 * @param props React props.
 */
const Banner: React.FC<BannerProps> = (props) => {
	const { backgroundImageRef, title, description, ctaLabel, ctaLink, className } = props;

	const imageInfo = fileRef.parse(backgroundImageRef);
	const backgroundStyle = {
		backgroundImage: `url(${fileRef.getUrl(backgroundImageRef)})`,
		backgroundPosition: imageInfo ? `${imageInfo.focalX}% ${imageInfo.focalY}%` : undefined
	};

	let bannerClasses = ['ui-promo-banner', 'ui-component--full-width'];

	if (className?.length) {
		bannerClasses.push('ui-component--no-margin');
	}

	return (
		<div className={bannerClasses.join(' ')} style={backgroundStyle}>
			<div className="ui-promo-banner__internal">
				{!!title && <>
					<h3 className="ui-promo-banner__title">
						{title}
					</h3>
				</>}
				{!!description && <>
					<Html>
						{description}
					</Html>
				</>}
				{!!ctaLabel && !!ctaLink && <>
					<Link href={ctaLink} variant="primary">
						{ctaLabel}
					</Link>
				</>}
			</div>
		</div>
	);
}

export default Banner;