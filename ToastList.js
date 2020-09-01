// Module import examples - none are required:
import getRef from 'UI/Functions/GetRef';

export default class ToastList extends React.Component {
	
	close(toastInfo){
		var {toasts} = global.app.state;
		toasts = toasts.filter(toast => toast != toastInfo);
		global.app.setState({toasts});
	}
	
	render(){
		
		var { toasts } = global.app.state;
		
		if(!toasts || !toasts.length){
			return;
		}
		
		var renderFunc = this.props.children;
		
		if(Array.isArray(renderFunc)){
			if(renderFunc.length){
				renderFunc = renderFunc[0];
			}else{
				renderFunc = null;
			}
		}
		
		return <div className="toast-list">
			{
				toasts.map(toastInfo => {
					
					if(renderFunc){
						return renderFunc(toastInfo, () => this.close(toastInfo));
					}
					
					return <div className="toast fade show" role="alert" aria-live="assertive" aria-atomic="true">
					  <div className="toast-header">
						{toastInfo.iconRef && <img src={getRef(toastInfo.iconRef, {size: 32})} className="rounded mr-2" alt={toastInfo.description} /> }
						<strong className="mr-auto">{toastInfo.title}</strong>
						{ /*<small>11 mins ago</small>*/}
						<button type="button" className="ml-2 mb-1 close" data-dismiss="toast" aria-label="Close" onClick={() => this.close(toastInfo)}>
						  <span aria-hidden="true">&times;</span>
						</button>
					  </div>
					  <div className="toast-body">
						{toastInfo.description}
					  </div>
					</div>;
					
				})
			}
		</div>;
		
	}
	
}