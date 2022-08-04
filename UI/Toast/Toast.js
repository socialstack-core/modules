import { useToast } from 'UI/Functions/Toast';
import { useRouter } from 'UI/Session';
import CloseButton from 'UI/CloseButton';

const TOAST_PREFIX = 'toast';
const DEFAULT_VARIANT = 'info';

const supportedVariants = [
	'primary',
	'secondary',
	'success',
	'danger',
	'warning',
	'info',
	'light',
	'dark'
];

const variantAliases = [
	{
		variant: 'primary',
		aliases: [
		]
	},
	{
		variant: 'secondary',
		aliases: [
		]
	},
	{
		variant: 'success',
		aliases: [
			'successful',
			'ok',
			'good'
		]
	},
	{
		variant: 'danger',
		aliases: [
			'fail',
			'failed',
			'failure',
			'error'
		]
	},
	{
		variant: 'warning',
		aliases: [
			'warn'
		]
	},
	{
		variant: 'info',
		aliases: [
			'information',
			'note'
		]
	},
	{
		variant: 'light',
		aliases: [
		]
	},
	{
		variant: 'dark',
		aliases: [
		]
	}
];

/**
 * Bootstrap Toast component
 */
export default function Toast(props) {
	const { toastInfo } = props;
	const { close } = useToast();
	const { setPage} = useRouter();

	var variant = toastInfo.variant || DEFAULT_VARIANT;
	var toastVariant = variant.toLowerCase();

	var aliases = variantAliases.filter(i => i.aliases.includes(toastVariant));

	if (aliases.length) {
		toastVariant = aliases[0].variant;
	}

	if (!supportedVariants.includes(toastVariant)) {
		toastVariant = DEFAULT_VARIANT;
	}

	var toastClass = [TOAST_PREFIX];
	toastClass.push(TOAST_PREFIX + '--' + toastVariant);

	toastClass.push('fade');
	toastClass.push('show');

	return (
		<div className={toastClass.join(' ')} role="alert" aria-live="assertive" aria-atomic="true">
			<div className="toast-header">
				<p className="toast-title">
					{toastInfo.iconRef && <img src={getRef(toastInfo.iconRef, { size: 32 })} className="rounded mr-2" alt={toastInfo.description} />}
					<strong className="toast-title">
						{toastInfo.title}
					</strong>
				</p>
				<CloseButton callback={() => close(toastInfo)} />
			</div>
			<div className="toast-body" onClick={() => {
				if (toastInfo.url) {
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
