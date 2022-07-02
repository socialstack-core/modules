const BUTTON_GROUP_PREFIX = 'btn-group';

export default function ButtonGroup(props) {
	const { children, label, size, isVertical } = props;

	var btnGroupClass = [isVertical ? (BUTTON_GROUP_PREFIX + '-vertical') : BUTTON_GROUP_PREFIX];

	if (size && size.length) {
		btnGroupClass.push(BUTTON_GROUP_PREFIX + size);
    }
	
	return (
		<div className={btnGroupClass.join(' ')} role="group" aria-label={label}>
			{children}
		</div>
	);
}

ButtonGroup.propTypes = {
	label: 'string',
	size: [
		{ name: 'Small', value: '-sm' },
		{ name: 'Normal', value: '' },
		{ name: 'Large', value: '-lg' }
	],
	isVertical: 'bool'
};

ButtonGroup.defaultProps = {
	//label: '',
	size: '',
	isVertical: false
}

ButtonGroup.icon = 'align-center';
