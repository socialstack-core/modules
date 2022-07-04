// Module import examples - none are required:
import getRef from 'UI/Functions/GetRef';
import {useToast} from 'UI/Functions/Toast';
import { useRouter } from 'UI/Session';


export default function ToastList (props){
	const { toastList, close } = useToast();
	const {setPage} = useRouter();
	
	var renderFunc = props.children;
	
	if(Array.isArray(renderFunc)){
		if(renderFunc.length){
			renderFunc = renderFunc[0];
		}else{
			renderFunc = null;
		}
	}
	
	return <div className="toast-list">
		{
			toastList.map(toastInfo => {
				
				if(renderFunc){
					return renderFunc(toastInfo, () => close(toastInfo));
				}
				
				return <div className="toast fade show" role="alert" aria-live="assertive" aria-atomic="true">
					<div className="toast-header">
					{toastInfo.iconRef && <img src={getRef(toastInfo.iconRef, {size: 32})} className="rounded mr-2" alt={toastInfo.description} /> }
					<strong className="mr-auto">{toastInfo.title}</strong>
					{ /*<small>11 mins ago</small>*/}
					<button type="button" className="ml-2 mb-1 close" data-dismiss="toast" aria-label="Close" onClick={() => close(toastInfo)}>
						<span aria-hidden="true">&times;</span>
					</button>
					</div>
					<div className="toast-body" onClick = {() => {
						if(toastInfo.url) {
						setPage(toastInfo.url);
					}
					}}>
					{toastInfo.description}
					</div>
				</div>;
				
			})
		}
	</div>;
}