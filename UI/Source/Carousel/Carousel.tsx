//import Button from 'UI/Button';
import { useState, useEffect, useRef } from 'react';

/**
 * Props for the Carousel component.
 */
interface CarouselProps {
	/**
	 * unique ID
	 */
	id?: string,

	/**
	 * set true to render offset to the right / top (useful to ensure back button not clipped if rendered within a parent without overflow)
	 */
	offset?: boolean,

	/**
	 * set true to render back button above images
	 */
	overlayArrow?: boolean,

	/**
	 * set true to render disabled
	 */
	disabled?: boolean,

	/**
	 * optional additional classnames
	 */
	className?: string,

	/**
	 * child items for carousel
	 */
	children?: React.ReactNode,

	/** 
	 * optional aria-label prop
	 */
	ariaLabel?: string,

	/**
	 * optional aria-labelled-by prop (i.e. ID of related label)
	 */
	ariaLabelledBy?: string

	/**
	 * NB: item width / height values should be defined via CSS on the carousel as:
	 * --ui-carousel-item-width
	 * --ui-carousel-item-height
	 * 
	 * this allows us to override the item size across various breakpoints if needed
	 */
}

/**
 * The Carousel React component.
 * @param props React props.
 */
const Carousel: React.FC<CarouselProps> = (props) => {
	const { offset, overlayArrow, disabled, className, children, ariaLabel, ariaLabelledBy } = props;
	const id = props.id || "carousel";
	const itemsId = `${id}_items`;

	const carouselRef = useRef();
	const scrollContainerRef = useRef();
	const btnBackRef = useRef();
	const btnNextRef = useRef();

	const [scrollBehaviour, setScrollBehaviour] = useState<string>('smooth');

	useEffect(() => {

		if (!carouselRef?.current) {
			return;
		}

		setScrollBehaviour(window?.matchMedia("(prefers-reduced-motion: reduce)").matches ? 'instant' : 'smooth');

		carouselRef.current.addEventListener("keydown", keyHandler);
		carouselRef.current.addEventListener('wheel', wheelHandler);

		if (btnBackRef?.current) {
			btnBackRef.current.addEventListener("keydown", keyHandler);
			btnBackRef.current.addEventListener('wheel', wheelHandler);
		}

		if (btnNextRef?.current) {
			btnNextRef.current.addEventListener("keydown", keyHandler);
			btnNextRef.current.addEventListener('wheel', wheelHandler);
		}

		window.addEventListener('resize', updateButtons);

		if (!scrollContainerRef?.current) {
			return;
		}

		scrollContainerRef.current.addEventListener("scroll", updateButtons);

		// use intersectionObserver to gradually fade out items not fully visible
		const observer = new IntersectionObserver((entries) => {
			entries.forEach(entry => {
				// entry.intersectionRatio is between 0 and 1
				// Map it directly to opacity
				entry.target.style.opacity = entry.intersectionRatio;
			});
		}, {
			root: scrollContainerRef.current,
			threshold: Array.from({ length: 101 }, (_, i) => i / 100) // 0, 0.01, ..., 1
		});

		const children = scrollContainerRef.current.querySelectorAll(':scope > *');
		children.forEach((item) => observer.observe(item));

		return () => {

			if (carouselRef?.current) {
				carouselRef.current.removeEventListener("keydown", keyHandler);
				carouselRef.current.removeEventListener("wheel", wheelHandler);
			}

			if (btnBackRef?.current) {
				btnBackRef.current.removeEventListener("keydown", keyHandler);
				btnBackRef.current.removeEventListener("wheel", wheelHandler);
			}

			if (btnNextRef?.current) {
				btnNextRef.current.removeEventListener("keydown", keyHandler);
				btnNextRef.current.removeEventListener("wheel", wheelHandler);
			}

			window.removeEventListener('resize', updateButtons);

			if (scrollContainerRef?.current) {
				scrollContainerRef.current.removeEventListener("scroll", updateButtons);

				if (observer && children?.length) {
					children.forEach((item) => observer.unobserve(item));
					observer.disconnect();
				}
			}

		}
	}, []);

	useEffect(() => {
		// TODO: investigate possibility of retrieving item width/height values from first child
		updateButtons();
	}, [children]);

	function isVertical() {

		if (!scrollContainerRef?.current) {
			return false;
		}

		return window.getComputedStyle(scrollContainerRef.current).flexDirection === 'column';
	}

	function getScrollAmount() {

		if (!carouselRef?.current) {
			return 0;
		}

		const style = window.getComputedStyle(carouselRef.current);

		return getSizeInPixels(style, isVertical() ? "--ui-carousel-item-height" : "--ui-carousel-item-width");
	}

	function getSizeInPixels(computedStyle, varName) {
		const size = computedStyle.getPropertyValue(varName);

		if (!size || !size.length) {
			return 0;
		}

		const match = size.toLowerCase().trim().match(/^(-?[\d.]+)([a-z%]*)$/i);

		if (!match) {
			return 0;
		}

		return parseFloat(match[1]) * (match[2] == 'rem' ? 16 : 1);
	}

	function scrollCarousel(amount) {

		if (!scrollContainerRef?.current) {
			return;
		}

		if (isVertical()) {
			scrollContainerRef.current.scrollBy({ top: amount, behavior: scrollBehaviour });
		} else {
			scrollContainerRef.current.scrollBy({ left: amount, behavior: scrollBehaviour });
		}

	}

	function updateButtons() {

		if (!scrollContainerRef?.current || !btnBackRef.current || !btnNextRef.current) {
			return;
		}

		if (isVertical()) {
			const maxScrollTop = scrollContainerRef.current.scrollHeight - scrollContainerRef.current.clientHeight;

			if (scrollContainerRef.current.scrollTop <= 0) {
				if (btnBackRef.current == document.activeElement) {
					btnNextRef.current.focus();
				}

				btnBackRef.current.style.display = 'none';
			} else {
				btnBackRef.current.style.display = 'block';
			}

			if (scrollContainerRef.current.scrollTop >= maxScrollTop) {
				if (btnNextRef.current == document.activeElement) {
					btnBackRef.current.focus();
				}

				btnNextRef.current.style.display = 'none';
			} else {
				btnNextRef.current.style.display = 'block';
			}

		} else {
			const maxScrollLeft = scrollContainerRef.current.scrollWidth - scrollContainerRef.current.clientWidth;

			if (scrollContainerRef.current.scrollLeft <= 0) {
				if (btnBackRef.current == document.activeElement) {
					btnNextRef.current.focus();
				}

				btnBackRef.current.style.display = 'none';
			} else {
				btnBackRef.current.style.display = 'block';
			}

			if (scrollContainerRef.current.scrollLeft >= maxScrollLeft) {
				if (btnNextRef.current == document.activeElement) {
					btnBackRef.current.focus();
				}

				btnNextRef.current.style.display = 'none';
			} else {
				btnNextRef.current.style.display = 'block';
			}

		}

	}

	function keyHandler(e) {

		if (!scrollContainerRef?.current) {
			return;
		}

		const scrollAmount = getScrollAmount();
		const scrollSize = isVertical() ?
			scrollContainerRef.current.scrollHeight : scrollContainerRef.current.scrollWidth;
		const clientSize = isVertical() ?
			scrollContainerRef.current.clientHeight : scrollContainerRef.current.clientWidth;

		switch (e.key) {
			case 'Home':
				scrollCarousel(-scrollSize);
				e.preventDefault();
				break;

			case 'End':
				scrollCarousel(scrollSize);
				e.preventDefault();
				break;

			case 'PageUp':
				scrollCarousel(-clientSize);
				e.preventDefault();
				break;

			case 'PageDown':
				scrollCarousel(clientSize);
				e.preventDefault();
				break;

			case 'ArrowLeft':

				if (!isVertical()) {
					scrollCarousel(-scrollAmount);
					e.preventDefault();
				}

				break;

			case 'ArrowUp':

				if (isVertical()) {
					scrollCarousel(-scrollAmount);
					e.preventDefault();
				}

				break;

			case 'ArrowRight':

				if (!isVertical()) {
					scrollCarousel(scrollAmount);
					e.preventDefault();
				}

				break;

			case 'ArrowDown':

				if (isVertical()) {
					scrollCarousel(scrollAmount);
					e.preventDefault();
				}

				break;
		}

	};

	function wheelHandler(e) {
		e.preventDefault();

		const scrollAmount = getScrollAmount();
		scrollCarousel(e.deltaY < 0 ? -scrollAmount : scrollAmount);
	}

	const baseClass = 'ui-carousel';
	let classNames = [baseClass];

	if (offset) {
		classNames.push(`${baseClass}--offset`);
	}

	if (overlayArrow) {
		classNames.push(`${baseClass}--overlay-arrow`);
	}

	if (className?.trim().length) {
		classNames.push(className);
	}

	return (
		<section className={classNames.join(' ')} aria-roledescription="carousel" ariaLabel={ariaLabel} ariaLabelledBy={ariaLabelledBy} id={id} ref={carouselRef}>
			<div className={`${baseClass}__internal`}>
				{/* commented until UI/Button ref forwarding support sorted
				<Button outlined className={`${baseClass}__back`} ariaControls={itemsId} aria-label={`Show previous items`} 
					onClick={() => scrollCarousel(-scrollAmount)} ref={btnBackRef}>
					<i className="fr fr-chevron-left"></i>
				</Button>
				*/}
				<button class="btn ui-btn btn-outline-primary ui-carousel__back" ariaLabel={`Show previous items`} type="button"
					ariaControls={itemsId} onClick={() => scrollCarousel(-getScrollAmount())} ref={btnBackRef}>
					<i class="fr fr-chevron-left"></i>
				</button>

				<div id={itemsId} className={`${baseClass}__scroll-container`} tabIndex="-1" aria-live="polite" ref={scrollContainerRef}>
					{children}
				</div>
				{/* commented until UI/Button ref forwarding support sorted
				<Button outlined className={`${baseClass}__next`} ariaControls={itemsId} aria-label={`Show next items`} 
					onClick={() => scrollCarousel(scrollAmount)} ref={btnNextRef}>
					<i className="fr fr-chevron-right"></i>
				</Button>
				*/}
				<button class="btn ui-btn btn-outline-primary ui-carousel__next" ariaLabel={`Show next items`} type="button"
					ariaControls={itemsId} onClick={() => scrollCarousel(getScrollAmount())} ref={btnNextRef}>
					<i class="fr fr-chevron-right"></i>
				</button>
			</div>
		</section>
	);
}

export default Carousel;