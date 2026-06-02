import { Product } from 'Api/Product';
import {useEffect, useMemo, useRef} from "react";
import Image from 'UI/Image';
import Video from 'UI/Video';
// @ts-ignore
import defaultImageRef from './image_placeholder.png';
import {Upload} from "Api/Upload";
import { isVideo } from 'UI/FileRef';
import CarouselWrapper from 'UI/Carousel';

export type CarouselItem = {
	
	// to compare against products
	id: uint | string,
	
	// holds the image ref
	featureRef?: string
	
	// holds the main content ref, if the featured version is different. If not set or unused, use featureRef.
	// For example a video cover image is displayed as the feature and the content is a video.
	contentRef?: string
} 

/**
 * Props for the Carousel component.
 */
interface CarouselProps {
	/**
	 * The content to display in this signpost.
	 */
	product: Product,

	/**
	 * A possibly selected variant
	 */
	currentVariant?: Product

	/**
	 * When a thumb is changed.
	 * @param item
	 */
	onThumbSelected?: (item: Product | CarouselItem) => void

	/**
	 * Passed through via props.
	 */
	selectedThumbnail?: Product | CarouselItem
}

const Carousel: React.FC<CarouselProps> = ({ product, currentVariant, onThumbSelected, selectedThumbnail }: CarouselProps) => {
	var activeProduct = currentVariant || product;
	
	// we use useMemo here as this is derived from props, doesn't need
	// any state, and should only mutate when changing products.
	const allItems: CarouselItem[] = useMemo(() => {
		
		// if product.variants is empty
		// nullish coalesce (i.e. fallback)
		// so we're always guaranteed an array
		// which means .map is always valid.
		const mainImages = (product.variants ?? [])
			// we want essentially 2 things from each variant, we want its ID, and we want its featureRef. 
			.map((variant) => ({ id: variant.id, featureRef: variant.featureRef }))
			// filter out any item where featureRef is falsy
			// i.e. empty string
			// i.e. null
			// i.e. undefined
			.filter(item => Boolean(item.featureRef));
		
		const additionalImages = ((product.productImages ?? []).map(((item, idx) => ({ id: product.id + '-additional-' + idx, featureRef: item.coverImageRef || item.ref, contentRef: item.ref }))))
			.filter(item => Boolean(item.featureRef))
		
		const productVariantsAdditionalImages = (product.variants ?? []).flatMap((variant, idx) => {
			return (variant.productImages ?? []).map((image: Upload, index: number) => {
				return ({
					id: variant.id + 'additional-variant-' + index,
					featureRef: image.coverImageRef || image.ref,
					contentRef: image.ref
				})
			})	
		});
		
		return [...mainImages, ...additionalImages, ...productVariantsAdditionalImages] as CarouselItem[];
		// product is its only dependency.
	}, [product])
	
	// this ref exists to add a scroll into view
	// call to the highlighted product, this scrolls
	// the pane towards whichever variant is selected. 
	const activeThumbnailRef = useRef<HTMLLabelElement>(null);
	
	useEffect(() => {
		if (activeThumbnailRef.current) {
			
			// the selected thumbnail when
			// selected from the matrix may be
			// at the end of the thumbnail list,
			// hidden by the containers constrained size
			// so here we scroll to the selected 
			// thumbnail.
			activeThumbnailRef.current.scrollIntoView({
				behavior: 'smooth', 
				block: 'nearest',  
				inline: 'nearest',
			})
		}
	}, [activeThumbnailRef.current])
	
	// taken from the original (unmodified), this removes the "open" attribute on the backing overlay
	const handleLightboxClick = (e: React.MouseEvent<HTMLDivElement>) => {
		if (e.target == e.currentTarget) {
			(e.target as HTMLDivElement).parentElement?.removeAttribute("open");
		}
	}
	
	// here we cascade back, 
	// in the parent component
	// when the attribute matrix is edited
	// it clears the "selectedThumbnail" state 
	// that gets passed to the corresponding prop. 
	// in that instance, it cascades down to 
	// current variant should one be selected,
	// this will definitely be true after the attribute matrix is selected. 
	// the only case where none of these are populated is where the 
	// page initially loads, which shows the default product image.
	const highlightedVariant = selectedThumbnail ?? currentVariant ?? product;
	
	// repeated helper closure, takes the currently selected picture
	// and renders it, when a featureRef isn't present, it renders
	// a default image. Mostly unmodified from its previous state
	// except it returns a full element as opposed to a fragment. 
	const renderImage = (item: Product | CarouselItem) => {
		
		if (item.featureRef) {
			const refIsVideo = (item as CarouselItem).contentRef && isVideo((item as CarouselItem).contentRef!, true);

			return (
				<details className="ui-product-images__slide">
					<summary>
						{refIsVideo && <>
							<svg className="ui-product-images__slide-play" aria-hidden="true" focusable="false" version="1.1" viewBox="-3 -3 30 30" xmlns="http://www.w3.org/2000/svg">
								<ellipse cx="12" cy="12" rx="15" ry="15" fill="#12121299" />
								<path d="m8.3011 18.775-0.27913-0.18914-0.0013-6.6062-0.0013-6.6062 0.57382-0.34689 10.574 6.6473-0.01 0.36162-0.01 0.36162-5.2654 3.2827c-2.896 1.8055-5.2735 3.283-5.2835 3.2835-0.01 4.49e-4 -0.1437-0.0843-0.29722-0.18832z" fill="#fff" />
							</svg>
						</>}
						<Image size={512} fileRef={item.featureRef} />
					</summary>
					<div className="ui-product-images__slide-content" onClick={(e) => handleLightboxClick(e)}>
						{refIsVideo ?
							<Video width={1024} fileRef={(item as CarouselItem).contentRef} autoHeight={true} autoplay={true} /> :
							<Image size={1024} fileRef={item.featureRef} lazyLoad={true} />
						}
					</div>
				</details>
			);
		}

		return <Image size={512} fileRef={defaultImageRef} className="ui-product-images__slide ui-product-images__slide--empty" />;
	}

	const hasRelatedImages = allItems.length != 0;
	let productImagesClasses = ["ui-product-images"];

	if (!hasRelatedImages) {
		productImagesClasses.push("ui-product-images--single");
	}
	
	const currentChosenIndex = allItems.findIndex((item) => item.featureRef === selectedThumbnail?.featureRef);
	
	return (
		<div className={productImagesClasses.join(' ')}>
			{hasRelatedImages && <>
				{/* hidden radio buttons (these drive image selection) */}
				<input type="radio" name="carousel" id={`carousel_1`} checked />
				{allItems.map((_, i) => {
					return <input type="radio" name="carousel" id={`carousel_${i+2}`} />
				})}
			
				{/* thumbnail images */}
				<CarouselWrapper id="product_images" offset={false} overlayArrow={true} className="ui-product-images__thumbnails"
					ariaLabel={`Product images`}>
					{/* 
						* this is the product's feature ref
						* and exists purely for the actual "product" not
						* a variant. 
					*/}
					<label
						onClick={() => onThumbSelected && onThumbSelected(product)}
						htmlFor={`current_1`}
						className={"ui-product-images__thumbnail " + (highlightedVariant.id === product.id ? 'active' : '')}
						ref={highlightedVariant.id === product.id ? activeThumbnailRef : undefined}
					>
						<Image
							size={100}
							fileRef={product.featureRef || defaultImageRef}
						/>
					</label>
					{/* variant iteration, this iterates a collection of carousel items that belong purely to variants */}
					{allItems.map((item, i) => {
						// when clicked, this sets the parent state to selected variant. 
						return (
							<label
								onClick={() => onThumbSelected && onThumbSelected(item)}
								htmlFor={`current_${i + 2}`}
								className={"ui-product-images__thumbnail " + (highlightedVariant?.id === item.id ? 'active' : '')}
								ref={highlightedVariant.id === item.id ? activeThumbnailRef : undefined}
							>
								<Image size={100} fileRef={item.featureRef || defaultImageRef} />
							</label>
						);
					})}
				</CarouselWrapper>
			</>}
			<div className="ui-product-images__slides">
				{renderImage(highlightedVariant)}
			</div>
		</div>
	)
	
}


export default Carousel;