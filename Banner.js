import getRef from 'UI/Functions/GetRef';

export default class Banner extends React.Component{
    render(){
		var col = this.props.fontColor || 'white';
        return(
			<div className="hero-banner" style ={{backgroundImage: "url("+getRef(this.props.backgroundRef, {url: true, size: 'original'})+")"}}>
				<div className="container h-100">
					<div className="row h-100 align-items-center">
						<div className="col-3"></div>
						<div className="col-7 banner-text">
							<h1 className="title" style={{color: col}} dangerouslySetInnerHTML={{__html: this.props.title}} />
							<br/>
							<br/>
							<p style={{color: col}} dangerouslySetInnerHTML={{__html: this.props.description}}/>
						</div>
					</div>
				</div>
			</div>
        );
    }
}

Banner.propTypes = {
	fontColor: 'color',
	backgroundRef: 'image',
	title: 'string',
	description: 'string'
};

Banner.icon='bullhorn';