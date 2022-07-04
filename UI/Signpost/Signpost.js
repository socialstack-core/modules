import getRef from 'UI/Functions/GetRef';

export default class Signpost extends React.Component{
	
	constructor(props){
		super(props);
	}
	
	render(){
		var { backgroundRef, mainTitle, target, callToAction } = this.props;
		
		return (
			<div className="card shadow-sm">
				<img src={getRef(backgroundRef, {url: true})} className="bd-placeholder-img card-img-top" width="100%" />
				<div className="card-body">
					<p className="card-text">{mainTitle}</p>
					<div className="d-flex justify-content-between align-items-center">
						<div className="btn-group">
							<a href={target}>
								<button type="button" className="btn btn-sm btn-outline-secondary">{callToAction || 'Go'}</button>
							</a>
						</div>
					</div>
				</div>
			</div>
		);
	}
}

Signpost.icon = 'fa-map-sign';
Signpost.rendererPropTypes = Signpost.propTypes = {
	backgroundRef: 'image',
	mainTitle: 'string',
	target: 'string',
	callToAction: 'string'
};