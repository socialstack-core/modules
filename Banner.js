import getRef from 'UI/Functions/GetRef';

export default Banner = props => {
	var {fontColor, backgroundRef, backgroundColor, title, description, height, textWidth} = props;

	var style = {
		minHeight: height || '80vh'
	};

	if(backgroundRef && backgroundRef.length){
		style.backgroundImage = "url("+getRef(backgroundRef, {url: true, size: 'original'})+")";
	}

	if(backgroundColor && backgroundColor.length){
		style.backgroundColor = backgroundColor;
	}

	if(!textWidth){
		textWidth = 6;
	}

	var halfWidth = (12-textWidth)/2;

	var textStyle = {};
	if(fontColor && fontColor.length){
		textStyle.color = fontColor;
	}

	return(
		<div className="hero-banner" style ={style}>
			<div className="container">
				<div className="row align-items-center">
					<div className={"col-" + halfWidth}></div>
					<div className={"col-" + textWidth + " banner-text"}>
						<h1 className="title" style={textStyle} dangerouslySetInnerHTML={{__html: title}} />
		  {description && <>}
						   <br/><br/>
			 <p style={textStyle} dangerouslySetInnerHTML={{__html: description}}/>
		   </>}
					</div>
				</div>
			</div>
		</div>
	);
}

Banner.propTypes = {
	fontColor: 'color',
	backgroundRef: 'image',
	backgroundColor: 'color',
	title: 'string',
	description: 'string',
	height: 'string',
	textWidth: [2,4,6,8,10,12]
};

Banner.icon='bullhorn';
