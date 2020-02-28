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
		var items = this.props.items;
		
		if(this.props.children && this.props.children.length && Array.isArray(this.props.children)){
			items = this.props.children;
		}
		
		return items;
	}
	
    render() {
		
		var items = this.content();
		
		if(!items || !items.length){
			return null;
		}
		
		var visCount = this.props.visibleAtOnce || 1;
		
		var itemSize = (1/visCount) * 100;
		var itemSizePs = itemSize + '%';
		
		var Module = this.props.renderBy;
		
		var imgSize = this.props.imageSize === '' ? undefined : this.props.imageSize || 1024;
		
		return (
			<div className="carousel">
				<div className="slider-container">
					<div className="slider">
						<ul className="content-list content" style={{marginLeft: (-itemSize * this.state.currentIndex) + '%'}}>
							{
								items.map(item => {
									
									var content = React.isValidElement(item) ? item : null;
									
									if(!content && Module){
										content = <Module {...item}/>;
									}
									
									if(!content){
										// Default.
										content = <div style={{
											height: (this.props.height ? (this.props.height + 'px') : '400px'),
											backgroundColor: item.color,
											backgroundImage: item.backgroundRef ? 'url(' + getRef(item.backgroundRef, {url: true, size: item.size || imgSize}) + ')' : undefined,
											backgroundSize: this.props.spreadImage ? 'cover' : 'contain',
											backgroundPosition: 'center center',
											backgroundRepeat: 'no-repeat'
										}}>
											<center>
												{
													item.mainTitle && (<h1>{item.mainTitle}</h1>)
												}
												{
													item.subTitle && (<h2>{item.subTitle}</h2>)
												}
												{
													item.description && (<p>{item.description}</p>)
												}
											</center>
										</div>;
									}
									
									return (
										<li className="content-item" style={{width: itemSizePs}}>
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

Carousel.propTypes = {
	delay: 'int',
	imageSize: 'int',
	items: {
		type: 'set',
		element: {
			mainTitle: 'string',
			subTitle: 'string',
			description: 'string',
			backgroundRef: 'image',
			color: 'color'
		}
	},
	spreadImage: 'bool',
	visibleAtOnce: {type: 'int', default: 1},
	renderBy: 'component'
};