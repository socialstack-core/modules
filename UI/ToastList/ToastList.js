import Toast from 'UI/Toast';
import {useToast} from 'UI/Functions/Toast';

export default function ToastList (props){
	const { toastList, close } = useToast();
	
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
					return renderFunc(toastInfo, () => close(toastInfo));ex
				}

				return <Toast toastInfo={toastInfo}></Toast>;
				
			})
		}
	</div>;
}