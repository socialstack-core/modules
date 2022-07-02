import getRef from 'UI/Functions/GetRef';

// Default carousel item renderer.
export default class Default extends React.Component{
	
	constructor(props){
		super(props);
	}
	
	render(){
		var host = {}; // this.props.__canvas.container || {};
		
		var {
			color,
			backgroundRef,
			mainTitle,
			subTitle,
			description,
			size
		} = this.props;
		
		var imgSize = host.imageSize === '' ? undefined : host.imageSize;
		
		return <div style={{
			height: (host.height ? (host.height + 'px') : '400px'),
			backgroundColor: color,
			backgroundImage: backgroundRef ? 'url(' + getRef(backgroundRef, {url: true, size: size || imgSize}) + ')' : undefined,
			backgroundSize: host.spreadImage ? 'cover' : 'contain',
			backgroundPosition: 'center center',
			backgroundRepeat: 'no-repeat'
		}}>
			<center>
				{
					mainTitle && (<h1>{mainTitle}</h1>)
				}
				{
					subTitle && (<h2>{subTitle}</h2>)
				}
				{
					description && (<p>{description}</p>)
				}
			</center>
		</div>;
	}
	
}

Default.moduleSet = 'renderer';

Default.propTypes = {
	color: 'color',
	backgroundRef: 'image',
	mainTitle: 'string',
	subTitle: 'string',
	description: 'string'
};