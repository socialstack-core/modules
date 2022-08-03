import { useToast } from 'UI/Functions/Toast';
import { useRouter } from 'UI/Session';

export default function Toast(props) {

	const {close } = useToast();
	const {setPage} = useRouter();

	var toastInfo = props.toastInfo;

	return (
		<div className="toast fade show" role="alert" aria-live="assertive" aria-atomic="true">
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
		</div>
	);
}

Toast.propTypes = {
};

Toast.defaultProps = {
}

Toast.icon = 'align-center';
