import Icon from 'UI/Icon';


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
	
	renderButton(left){
		return <span className="chev-circle">
			{left ? <Icon type='chevron-left' light/> : <Icon type='chevron-right' light/>}
		</span>;
	}
	
	containerWidth(screenWidth)
	{
		if(screenWidth >= 1400) {
			return 1320;
		} else if (screenWidth >= 1200) {
			return 1140;
		} else if (screenWidth >= 992) {
			return 960;
		} else if (screenWidth >= 768) {
			return 720;
		} else if (screenWidth >= 576) {
			return 540;
		}

		return screenWidth;
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
			spacing,
			toggleOpacity
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
        var itemInternalStyle = null;
		
		if(spacing){
			padNum = parseInt(spacing);
			if(!isNaN(padNum)){
                spacing = (padNum /2) + 'px';
			}
		}

		if (centred) {

            if (spacing) {
                itemInternalStyle = { marginRight: spacing, marginLeft: spacing };
            }

		}

		//var contentClass = this.state.currentIndex === 0 ? "content-list content first" : "content-list content";
		var contentClass = toggleOpacity ? "content-list content first toggle-opacity" : "content-list content first";
		var width = 0;
		
		// TODO: update media query matches in realtime
		if (window.matchMedia('(max-width: 575px)').matches) {
			width = window.innerWidth - 32;
		}

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
		
		var screenWidth = window.innerWidth || window.screen.width;
		var container = this.containerWidth(screenWidth);
		var containerLeftEdge = (screenWidth/2) - (container/2);
		
		// hide controls if nothing to page
		if (visCount >= items.length) {
			showBack = false;
			showNext = false;
		}

		var transformCalc = '';
		var slideWidthCalc = '';
		var slideWidth = '';
			
		transformCalc = spacing ?
			(width / visCount) + (padNum / visCount) :
			width / visCount;
		slideWidthCalc = transformCalc + "px";
		slideWidth = { flex: "0 0 " + slideWidthCalc };
		
		var slideOffset = centred ?
			{ marginLeft: (containerLeftEdge - (transformCalc * this.state.currentIndex)) + 'px' } :
			{ marginLeft: '-' + transformCalc + 'px' };
		
		return (
			<div className="carousel" data-theme={this.props['data-theme']}>
				<div className="slider-container">
					<div className={sliderClass}>
						{showBack && this.state.currentIndex > 0 &&
							<div className="slider-back-wrapper">
								<button type="button" className="slider-back" onClick={() => {
									this.movePrevious();
								}}>
									{this.renderButton(true)}
								</button>
							</div>
						}
						<div className="container-offset" style={slideOffset}>
							<ul className={contentClass} data-offset={this.state.currentIndex}>
								{
									items.map((item,i) => {
										var content = React.isValidElement(item) ? item : null;
										
										if(this.props.children && this.props.children.length){
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
						</div>
						{showNext && this.state.currentIndex < items.length - 1 &&
							<div className="slider-next-wrapper">
								<button type="button" className="slider-next" onClick={() => {
									this.moveNext();
								}}>
									{this.renderButton(false)}
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