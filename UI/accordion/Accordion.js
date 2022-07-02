// WIP
// TODO: https://getbootstrap.com/docs/5.1/components/collapse/#accessibility

const ACCORDION_PREFIX = 'accordion';

/**
 * Bootstrap Accordion component
 * @param {any} children		- content
 * @param {boolean} isFlush		- set true to remove background colour and some borders / radiused corners
 * @param {boolean} alwaysOpen	- set true to make accordion items stay open when another item is opened
 */
export default function Accordion(props) {
	const { children, isFlush, alwaysOpen } = props;

	var accordionClass = [ACCORDION_PREFIX];

	if (isFlush) {
		accordionClass.push(ACCORDION_PREFIX + '-flush');
	}

	return (<>
		<div className={accordionClass.join(' ')}>
			{children}
		</div>
	</>);
}

Accordion.propTypes = {
	isFlush: 'bool',
	alwaysOpen: 'bool'
};

Accordion.defaultProps = {
	isFlush: false,
	alwaysOpen: false
}

Accordion.icon = 'exclamation-circle';
