import getRef from 'UI/Functions/GetRef';


export default class Carousel extends React.Component {
    constructor(props) {
        super(props);
		
		this.state = {
			currentIndex: 0
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

	movePrevious(context) {
		this.moveTo(((context.currentIndex || 0) <= 0) ? this.count() - 1 : (this.state.currentIndex || 0) - 1);
	}
	
	componentDidMount(){
		if(this.interval){
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
			visibleAtOnce,
			spacing
		} = this.props;
		
		var items = this.content();
		
		if(!items || !items.length){
			return null;
		}
		
        var visCount = visibleAtOnce || 1;

        if (window.matchMedia('(max-width: 767px)').matches && visCount > 2) {
            visCount = 2;
        }

        if (window.matchMedia('(max-width: 575px)').matches) {
            visCount = 1;
        }

		var itemSize = (1/visCount) * 100;
		var itemSizePs = itemSize + '%';
		
		// The renderer is regularly a Canvas which performs a bunch of substitutions.
		var Module = items.renderer;
		
		if(spacing){
			var padNum = parseInt(spacing);
			if(!isNaN(padNum)){
				itemSizePs = 'calc(' + itemSizePs + ' - ' + (padNum * 2) + 'px)';
				spacing = padNum + 'px';
			}
		}
		
		return (
			<div className="carousel">
				<div className="slider-container">
					<div className="slider">
						<ul className="content-list content" style={{marginLeft: (-itemSize * this.state.currentIndex) + '%'}}>
							{
								items.map(item => {
									var content = React.isValidElement(item) ? item : null;
									
									if(!content && Module){
										content = <Module item={item} container={this.props}/>;
									}
									
									return (
										<li className="content-item" style={{width: itemSizePs, margin: spacing}}>
											{content}
										</li>
									);
									
								})
							}
						</ul>
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