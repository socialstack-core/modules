export default class Carousel extends React.Component {
    constructor(props) {
        super(props);
		
		this.state = {
			currentIndex: props.defaultSlide || 0
		};
    }
	
	moveTo(index) {
		var c = this.count();
		if(!c){
			return;
		}
		if(index >= c){
			index = c - 1;
		}else if(index < 0){
			index = 0;
		}
		
		this.setState({currentIndex: index});
	}
	
	moveNext() {
		this.moveTo(((this.state.currentIndex || 0) >= this.count() - 1) ? 0 : ((this.state.currentIndex || 0) + 1));
	}

	movePrevious() {
		this.moveTo(((this.state.currentIndex || 0) <= 0) ? this.count() - 1 : (this.state.currentIndex || 0) - 1);
	}
	
	componentDidMount() {
		if(this.interval || this.props.static){
			return;
		}
		
		this.interval = setInterval(() => {
			this.moveNext();
		}, (this.props.delay || 3) * 1000);
	}
	
	componentWillUnmount(){
		this.interval && clearInterval(this.interval);
		this.interval = null;
	}
	
	count(){
		var set = this.content();
		return set?set.length : 0;
	}
	
	content(){
		return this.props.items || [];
	}
	
    render() {
		
		var {
			showBack, // show back link
			showNext, // show next link
			// treat current page as held within a <Container>, even though it isn't
			// (this allows us to align the current page to the centre of the screen, but show other content either side)
			centred,
			visibleAtOnce,
			visibleAtOnceSm,
			visibleAtOnceMd,
			visibleAtOnceLg,
			spacing
		} = this.props;

		var items = this.content();
		
		if(!items || !items.length){
			return null;
		}
		
        var visCount = visibleAtOnce || 1;
		var visCountSm = visibleAtOnceSm || visCount;
		var visCountMd = visibleAtOnceMd || visibleAtOnceSm;
		var visCountLg = visibleAtOnceLg || visibleAtOnceMd;

		var sliderClass = centred ? "slider slider-align-first" : "slider";
		
		// The renderer is regularly a Canvas which performs a bunch of substitutions.
		var Module = items.renderer;
        var padNum = 0;
        var itemInternalStyle = '';
		
		if(spacing){
			padNum = parseInt(spacing);
			if(!isNaN(padNum)){
                spacing = padNum + 'px';
                itemStyle = { margin: spacing };
			}
		}

		if (centred) {

            if (spacing) {
                itemInternalStyle = { marginRight: spacing };
            }

		}

		var contentClass = this.state.currentIndex === 0 ? "content-list content first" : "content-list content";
		var width = 0;

		// TODO: update media query matches in realtime
		if (window.matchMedia('(min-width: 576px)').matches) {
			visCount = visCountSm || visCount;
			width = 510;
		}

		if (window.matchMedia('(min-width: 768px)').matches) {
			visCount = visCountMd || visCount;
			width = 690;
		}

		if (window.matchMedia('(min-width: 992px)').matches) {
			visCount = visCountLg || visCount;
			width = 930;
		}

		var slideWidthCalc = '';
		var slideWidth = '';

		if (centred) {
			slideWidthCalc = spacing ?
				"calc( (" + width + "px / " + visCount + ") + (" + spacing + " / " + visCount + ") )" :
				"calc(" + width + "px / " + visCount + ")";
			slideWidth = { flex: "0 0 " + slideWidthCalc };
		}

		var slideOffset = centred ?
			//{ marginLeft: "-" + this.state.currentIndex + " * calc(" + width + "px / " + visCount + ")" } :
			{ marginLeft: "calc(-(" + this.state.currentIndex + " * " + width / visCount + "px) / " + visCount + ")" } :
			{ marginLeft: (-((1 / visCount) * 100) * this.state.currentIndex) + '%' };

		//console.log("width: " + width / visCount + spacing / 2);
		// transform: translateX(calc(-473px * this.state.currentIndex))

		return (
			<div className="carousel">
				<div className="slider-container">
					<div className={sliderClass}>
						{showBack && this.state.currentIndex > 0 &&
							<div className="slider-back-wrapper">
								<button type="button" className="slider-back" onClick={() => {
									this.movePrevious();
								}}>
									{/* effectively fa-chevron-left without the icon font dependency */}
									<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 474.133 474.133" height="1792" width="1792">
										<path d="M346.918 435.883c11.485-11.554 11.485-30.214 0-41.768l-157-157 157-156.705c11.616-11.616 11.616-30.45 0-42.065-11.615-11.616-30.448-11.616-42.064 0L127.117 216.082c-11.486 11.554-11.486 30.214 0 41.769l177.737 177.736a29.61 29.61 0 0042.065.291z" fill="#111820" />
									</svg>
									<span className="sr-only">Back</span>
								</button>
							</div>
						}
						<ul className={contentClass} style={slideOffset}>
							{
								items.map((item,i) => {
									var content = React.isValidElement(item) ? item : null;
									
									if(this.props.children){
										content = this.props.children(item, i, this);
									}else if(!content && Module){
										content = <Module item={item} container={this.props}/>;
									}
									
                                    return (
										<li className="content-item" style={slideWidth}>
                                            <div className="content-item-internal" style={itemInternalStyle}>
                                                {content}
                                            </div>
										</li>
									);
									
								})
							}
						</ul>
						{showNext && this.state.currentIndex < items.length &&
							<div className="slider-next-wrapper">
								<button type="button" className="slider-next" onClick={() => {
									this.moveNext();
								}}>
									{/* effectively fa-chevron-right without the icon font dependency */}
									<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 474.133 474.133" height="1792" width="1792">
										<path d="M127.215 38.25c-11.485 11.555-11.485 30.214 0 41.768l157 157-157 156.706c-11.616 11.616-11.616 30.448 0 42.064 11.615 11.616 30.448 11.616 42.064 0l177.737-177.736c11.486-11.555 11.486-30.215 0-41.77L169.279 38.547a29.61 29.61 0 00-42.065-.291z" fill="#111820" />
									</svg>
									<span className="sr-only">Back</span>
								</button>
							</div>
						}
					</div>
				</div>
			</div>
		);
		
    }
}

Carousel.icon = 'columns';
Carousel.propTypes = {
	delay: 'int',
	imageSize: 'int',
	items: {
		type: 'set',
		defaultRenderer: 'UI/Carousel/Default'
	},
	spreadImage: 'bool',
	spacing: 'int',
	visibleAtOnce: {type: 'int', default: 1}
};