import Toast from 'UI/Toast';
import { useToast } from 'UI/Functions/Toast';

export default function ToastList(props) {
	const { horizontal, vertical } = props;

	const { toastList, close } = useToast();

	var renderFunc = props.children;

	if (Array.isArray(renderFunc)) {
		if (renderFunc.length) {
			renderFunc = renderFunc[0];
		} else {
			renderFunc = null;
		}
	}

	var toastClass = ['toast-list'];

	switch (horizontal) {
		case 'Left':
			toastClass.push('toast-list--left');
			break;

		case 'Centre':
			toastClass.push('toast-list--centre-x');
			break;

		default:
			toastClass.push('toast-list--right');
			break;
	}

	switch (vertical) {
		case 'Top':
			toastClass.push('toast-list--top');
			break;

		case 'Centre':
			toastClass.push('toast-list--centre-y');
			break;

		default:
			toastClass.push('toast-list--bottom');
			break;
    }

	return <div className={toastClass.join(' ')}>
		{
			toastList.map(toastInfo => {

				if (renderFunc) {
					return renderFunc(toastInfo, () => close(toastInfo));
				}

				return <Toast toastInfo={toastInfo}></Toast>;
			})
		}
	</div>;
}

ToastList.propTypes = {
	horizontal: [
		'Left',
		'Centre',
		'Right'
	],
	vertical: [
		'Top',
		'Centre',
		'Bottom'
	]
};

ToastList.defaultProps = {
	horizontal: 'Right',
	vertical: 'Bottom'
}

ToastList.icon = 'align-center'; // fontawesome icon
