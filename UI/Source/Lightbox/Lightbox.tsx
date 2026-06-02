import Button from 'UI/Button';
import Popover from 'UI/Popover';
import { useState } from 'react';
import * as fileRef from 'UI/FileRef';

/**
 * Props for the Lightbox component.
 */
interface LightboxProps {
	/**
	 * optional title
	 */
	title?: string,

	/**
	 * preferred size for thumbnail images (defaults to 256px)
	 */
	smallSize?: string,

	/**
	 * preferred size for full-size images (defaults to 1024px)
	 */
	largeSize?: string,

	/**
	 * field name referencing thumbnail image (defaults to 'small')
	 */
	smallField?: string,

	/**
	 * field name referencing full-size image (defaults to 'large')
	 */
	largeField?: string,

	/**
	 * field name referencing associated text for each image (defaults to 'alt')
	 */
	altField?: string,

	/**
	 * images can be supplied either as an array of objects;
	 * each object should include:
	 * - thumbnail image URL
	 * - full-size image URL
	 * - associated alt text (optional)
	 */
	images?: Record<string, string>[],

	/**
	 * ... or as an array of FileRefs
	 */
	imageRefs?: FileRef[]
}

/**
 * The Lightbox React component.
 * @param props React props.
 */
const Lightbox: React.FC<LightboxProps> = ({
	title = '',
	images = [],
	imageRefs = [],
	smallSize = '256',
	largeSize = '1024',
	smallField = 'small',
	largeField = 'large',
	altField = 'alt'
}) => {
	const [imageInfo, setImageInfo] = useState<object[]>([]);

	const populateImages = (source: string[]) => {
		var _images: object[] = [];

		source.forEach(ref => {
			var info: Record<string, string> = {};
			var small = fileRef.getUrl(ref, { size: smallSize });
			var large = fileRef.getUrl(ref, { size: largeSize });
			var parsed = fileRef.parse(ref);

			if (small?.length && large?.length) {
				info[smallField] = small;
				info[largeField] = large;

				if (parsed?.altText?.length) {
					info[altField] = parsed.altText;
				}

				_images.push(info);
			}

		});

		return _images;
	};

	if (imageRefs.length) {
		setImageInfo(populateImages(imageRefs));
	} else {

		if (images.length) {
			setImageInfo(images);
		}

	}

	if (!imageInfo.length) {
		return;
	}


	return (
		<section className="ui-lightbox">
			{/* define SVGs used within component */}
			<svg width="0" height="0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
				<defs>
					<g id="chevron_left">
						<path d="m15 18-6-6 6-6" />
					</g>
					<g id="chevron_right">
						<path d="m9 18 6-6-6-6" />
					</g>
				</defs>
			</svg>

			{/* display image collection as thumbnails;
				surrounding button acts as a trigger to display larger images within a popover
			  */}
			<Button outlined className="ui-lightbox__trigger" popoverTarget="lightbox_popover" aria-label={title?.length > 0 ? title : undefined}>
				<div className="ui-lightbox__thumbnails">
					{images.map((image, i) => {
						var imageId = `lightbox_slide${i+1}`;
						return <>
							<label htmlFor={imageId} className="ui-lightbox__thumbnail">
								<img src={image[smallField]} alt={image[altField]} />
							</label>
						</>;
					})}
				</div>
			</Button>

			{/* use popover for lightbox overlay */}
			<Popover className="ui-lightbox__overlay" id="lightbox_popover" method="manual" alignment="maximize" blurBackground={true}>
				<div className="ui-lightbox__overlay-focus">
					{/* use invisible radio buttons to control image selection */}
					{images.map((image, i) => {
						var imageId = `lightbox_slide${i+1}`;
						return <>
							{/* select the first image by default; 
								use of autoFocus here ensures the radio button collection grabs focus as soon as the overlay becomes visible
							  */}
							<input type="radio" name="lightbox" id={imageId} checked={i == 0 ? true : undefined} autoFocus={i == 0 ? true : undefined} />
						</>;
					})}

					{/* overlay header */}
					<header className="ui-lightbox__overlay-header">
						{title?.length > 0 && <>
							<h2 className="ui-lightbox__overlay-title">
								{title}
							</h2>
						</>}

						{/* close button */}
						<Button outlined className="ui-lightbox__overlay-close" popoverTarget="lightbox_popover" popoverTargetAction="hide">
							<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" strokeLinecap="round" strokeLinejoin="round">
								<path d="M18 6 6 18" />
								<path d="m6 6 12 12" />
							</svg>
						</Button>
					</header>
				</div>

				{/* full-size images (visibility controlled via radio button selection) */}
				<div className="ui-lightbox__slides">
					{images.map((image, i) => {
						var prevIdx = i;
						var nextIdx = i + 2;
						var prevId = `lightbox_slide${prevIdx == 0 ? images.length : prevIdx}`; 
						var nextId = `lightbox_slide${nextIdx > images.length ? 1 : nextIdx}`; 

						return <>
							<div className="ui-lightbox__slide">
								<label htmlFor={prevId} className="ui-lightbox__slide-control">
									<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
										<use xlinkHref="#chevron_left" />
									</svg>
								</label>
								<figure>
									<img loading="lazy" src={image[largeField]} alt={image[altField]} />
									<figcaption>
										{image[altField]?.length > 0 && <>
											{image[altField]}
											<br /><br />
										</>}
										<strong>
											{`Image ${i+1} of ${images.length}`}
										</strong>
									</figcaption>
								</figure>
								<label htmlFor={nextId} className="ui-lightbox__slide-control">
									<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
										<use xlinkHref="#chevron_right" />
									</svg>
								</label>
							</div>
						</>;
					})}
				</div>
			</Popover>
		</section>
	);
}

export default Lightbox;