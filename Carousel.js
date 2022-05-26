import { useState } from 'react';

const CAROUSEL_PREFIX = 'carousel';

/**
 * Bootstrap Carousel component
 * @param {array} slides                - array of slides (each with title, description, url and image)
 * @param {number} interval             - interval between each slide in ms (default 5000)
 * @param {boolean} fullBackgroundImage - set true to display images in the background (default is to show as a separate image above the caption)
 * @param {boolean} fade    	        - set true to fade between slides (default transition is to slide)
 * @param {boolean} showControls	    - set true to include left / right navigation buttons (default true)
 * @param {boolean} showIndicators	    - set true to include slide indicators (default true)
 * @param {boolean} showCta     	    - set true to include CTA button below each slide (default true)
 * @param {string} ctaLabel             - CTA button label text (default `Read more`)
 */
export default function Carousel(props) {
    const { slides, interval, fade, fullBackgroundImage, showControls, showIndicators, showCta, ctaLabel } = props;
    const [activeIndex, setActiveIndex] = useState(0);

    if (!slides || !slides.length) {
        return;
    }

    var carouselClass = [CAROUSEL_PREFIX];
    carouselClass.push(fade ? 'fade' : 'slide');

    return <>
        <div className={carouselClass.join(' ')}>
            {showIndicators && <>
                <div className="carousel-indicators">
                    {
                        slides.map((slide, index) => {
                            return (
                                <button type key={slide.id} aria-label={slide.title} data-bs-slide-to={index}
                                    className={index == activeIndex ? 'active' : undefined}
                                    aria-current={index == activeIndex ? true : undefined}>
                                </button>
                            );
                        })
                    }
                </div>
            </>}
            <div className="carousel-inner">
                {
                    slides.map((slide, index) => {
                        var cta = (slide.cta && slide.cta.length) ? slide.cta : ctaLabel;
                        return (
                            <div className={index == activeIndex ? 'carousel-item active' : 'carousel-item'}>

                                {!fullBackgroundImage && slide.image}

                                <div className="carousel-caption">
                                    <h5 class="carousel-caption__title">
                                        {slide.title}
                                    </h5>
                                    <p class="carousel-caption__description">
                                        {slide.description}
                                    </p>
                                    {showCta && cta && cta.length && <>
                                        <button type="button" class="btn btn-primary">
                                            {cta}
                                        </button>
                                    </>}
                                </div>
                            </div>
                        );
                    })
                }
            </div>
            {showControls && <>
                <button class="carousel-control-prev" type="button" data-bs-slide="prev">
                    <span class="carousel-control-prev-icon" aria-hidden="true"></span>
                    <span class="visually-hidden">
                        {`Previous`}
                    </span>
                </button>
                <button class="carousel-control-next" type="button" data-bs-slide="next">
                    <span class="carousel-control-next-icon" aria-hidden="true"></span>
                    <span class="visually-hidden">
                        {`Next`}
                    </span>
                </button>
            </>}
        </div>
    </>;
}

Carousel.propTypes = {
    interval: 'int',
    fade: 'bool',
    showControls: 'bool',
    showIndicators: 'bool',
    showIndicators: 'bool',
    ctaLabel: 'string'
};

Carousel.defaultProps = {
    interval: 5000,
    fade: false,
    showControls: true,
    showIndicators: true,
    showCta: true,
    ctaLabel: `Read more`
}

Carousel.icon = 'align-center';
