import { useState, useRef } from 'react';

const CAROUSEL_PREFIX = 'carousel';
const TRANSITION_END = 'transitionend';

/**
 * Bootstrap Carousel component
 * @param {array} slides                - array of slides (see below for structure)
 * @param {number} interval             - interval between each slide in ms (default 5000)
 * @param {boolean} fullBackgroundImage - set true to display images in the background (default is to show as a separate image above the caption)
 * @param {boolean} fade    	        - set true to fade between slides (default transition is to slide)
 * @param {boolean} showControls	    - set true to include left / right navigation buttons (default true)
 * @param {boolean} showIndicators	    - set true to include slide indicators (default true)
 * @param {boolean} showCta     	    - set true to include CTA button below each slide (default true)
 * @param {string} ctaLabel             - CTA button label text (default `Read more`)
 * 
 * example slides array:
 * [
 *   {
 *     title: 'Manager Updates',
 *     description: 'Introducing Cookidoo Served!',
 *     linkUrl: '#',
 *     imageUrl: 'https://d1icd6shlvmxi6.cloudfront.net/gsc/UBPVYH/b4/7d/3f/b47d3f8a02b54413b8aa4fb9218454d7/images/community_home_alternate/u0_state1.png?pageId=5090ad51-a18e-48d0-bd41-bb12d905a7f0'
 *    }
 * ]
 * 
 * NB: use 'jsx' property to define custom slide content
 */
export default function BootstrapCarousel(props) {
    const { slides, interval, fade, fullBackgroundImage, showControls, showIndicators, showCta, ctaLabel } = props;
    const [activeIndex, setActiveIndex] = useState(0);
    const carouselInnerRef = useRef(null);

    if (!slides || !slides.length) {
        return;
    }

    var carouselClass = [CAROUSEL_PREFIX];
    carouselClass.push(fade ? 'fade' : 'slide');

    if (!showControls) {
        carouselClass.push(CAROUSEL_PREFIX + "--no-controls");
    }

    var backgroundGradient = "linear-gradient(135deg, rgba(0,0,0,.75), rgba(0,0,0,.25))";

    function getSlide(index) {
        var slide = null;

        if (carouselInnerRef && carouselInnerRef.current) {
            var slides = carouselInnerRef.current.getElementsByClassName("carousel-item");

            if (slides && (slides.length - 1) >= index) {
                slide = slides[index];
            }
        }

        return slide;
    }

    function slideOffset(offset) {

        if (carouselInnerRef && carouselInnerRef.current) {
            var slides = carouselInnerRef.current.getElementsByClassName("carousel-item");

            if (slides) {
                var newIndex = (activeIndex + offset) % slides.length;

                if (newIndex < 0) {
                    newIndex = slides.length - 1;
                }

                moveTo(newIndex);
            }

        }

    }

    function moveTo(index) {
        console.log("moving from " + activeIndex + " to " + index);

        if (index == activeIndex) {
            return;
        }

        var fromSlide = getSlide(activeIndex);
        var toSlide = getSlide(index);

        if (!fromSlide || !toSlide) {
            return;
        }

        var carouselItemStartEnd = (index > activeIndex) ? "carousel-item-start" : "carousel-item-end";
        var carouselItemPrevNext = (index > activeIndex) ? "carousel-item-next" : "carousel-item-prev";

        if (!fade) {
            // slide
            //fromSlide.classList.add(carouselItemStartEnd);
            //toSlide.classList.add(carouselItemPrevNext, carouselItemStartEnd);

            toSlide.classList.add(carouselItemPrevNext);
            reflow(toSlide);

            fromSlide.classList.add(carouselItemStartEnd)
            toSlide.classList.add(carouselItemStartEnd)

            const completeCallBack = () => {
                toSlide.classList.remove(carouselItemStartEnd, carouselItemPrevNext);
                toSlide.classList.add("active");

                fromSlide.classList.remove("active", carouselItemPrevNext, carouselItemStartEnd);

                //this._isSliding = false
                //setTimeout(triggerSlidEvent, 0);
                setActiveIndex(index);
            }

            executeAfterTransition(completeCallBack, fromSlide, true);
        } else {
            // fade
            // TODO
        }

    }

    // restart element animation
    function reflow(element) {
        // eslint-disable-next-line no-unused-expressions
        element.offsetHeight;
    }

    function execute(callback) {
        if (typeof callback === 'function') {
            callback();
        }
    }

    function getTransitionDurationFromElement(element) {
        if (!element) {
            return 0;
        }

        // Get transition-duration of the element
        let {
            transitionDuration,
            transitionDelay
        } = window.getComputedStyle(element);
        const floatTransitionDuration = Number.parseFloat(transitionDuration);
        const floatTransitionDelay = Number.parseFloat(transitionDelay);

        // Return 0 if element or transition duration is not found
        if (!floatTransitionDuration && !floatTransitionDelay) {
            return 0;
        }

        // If multiple durations are defined, take the first
        transitionDuration = transitionDuration.split(',')[0];
        transitionDelay = transitionDelay.split(',')[0];

        return (Number.parseFloat(transitionDuration) + Number.parseFloat(transitionDelay)) * 1000;
    }

    function executeAfterTransition(callback, transitionElement, waitForTransition = true) {
        if (!waitForTransition) {
            execute(callback);
            return;
        }

        const durationPadding = 5;
        const emulatedDuration = getTransitionDurationFromElement(transitionElement) + durationPadding;
        let called = false;

        const handler = ({
            target
        }) => {
            if (target !== transitionElement) {
                return;
            }

            called = true;
            transitionElement.removeEventListener(TRANSITION_END, handler);
            execute(callback);
        };

        transitionElement.addEventListener(TRANSITION_END, handler);

        setTimeout(() => {
            if (!called) {
                transitionElement.dispatchEvent(new Event(TRANSITION_END));
            }
        }, emulatedDuration);
    }

    return <>
        <div className={carouselClass.join(' ')}>
            {showIndicators && <>
                <div className="carousel-indicators">
                    {
                        slides.map((slide, index) => {
                            return (
                                <button type key={slide.id} aria-label={slide.description} data-bs-target data-bs-slide-to={index}
                                    className={index == activeIndex ? 'active' : undefined}
                                    aria-current={index == activeIndex ? true : undefined}
                                    onClick={(e) => {
                                        var index = parseInt(e.currentTarget.dataset.bsSlideTo, 10);
                                        moveTo(index);
                                    }}>
                                </button>
                            );
                        })
                    }
                </div>
            </>}
            <div className="carousel-inner" ref={carouselInnerRef}>
                {
                    slides.map((slide, index) => {
                        var disableCta = slide.disableCta || !showCta;
                        var cta = (slide.cta && slide.cta.length) ? slide.cta : ctaLabel;
                        var slideStyle = fullBackgroundImage && slide.imageUrl ? { 'background-image': backgroundGradient + ', url(' + slide.imageUrl + ')' } : undefined;

                        return (
                            <div className={index == activeIndex ? 'carousel-item active' : 'carousel-item'} style={slideStyle}>

                                {!fullBackgroundImage && slide.imageUrl && !slide.jsx && <>
                                    <img src={slide.imageUrl} className="carousel-item__image" />
                                </>}

                                <div className="carousel-caption">
                                    {!slide.jsx && <>
                                        <h5 class="carousel-caption__title">
                                            {slide.title}
                                        </h5>
                                        <p class="carousel-caption__description">
                                            {slide.description}
                                        </p>
                                        {!disableCta && cta && cta.length && slide.linkUrl && <>
                                            <a href={slide.linkUrl} class="btn btn-primary">
                                                {cta}
                                            </a>
                                        </>}
                                    </>}

                                    {slide.jsx}
                                </div>
                            </div>
                        );
                    })
                }
            </div>
            {showControls && <>
                <button class="carousel-control-prev" type="button" data-bs-slide="prev"
                    onClick={(e) => {
                        slideOffset(-1);
                    }}>
                    <span class="carousel-control-prev-icon" aria-hidden="true"></span>
                    <span class="visually-hidden">
                        {`Previous`}
                    </span>
                </button>
                <button class="carousel-control-next" type="button" data-bs-slide="next"
                    onClick={(e) => {
                        slideOffset(1);
                    }}>
                    <span class="carousel-control-next-icon" aria-hidden="true"></span>
                    <span class="visually-hidden">
                        {`Next`}
                    </span>
                </button>
            </>}
        </div>
    </>;
}

BootstrapCarousel.propTypes = {
    interval: 'int',
    fade: 'bool',
    showControls: 'bool',
    showIndicators: 'bool',
    showIndicators: 'bool',
    ctaLabel: 'string'
};

BootstrapCarousel.defaultProps = {
    interval: 5000,
    fade: false,
    showControls: true,
    showIndicators: true,
    showCta: true,
    ctaLabel: `Read more`
}

BootstrapCarousel.icon = 'align-center';
