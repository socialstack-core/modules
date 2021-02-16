import omit from 'UI/Functions/Omit';

/**
 * A 12 segment responsive column. Usually used within a <Row>.
 */

export default function Column (props) {
	var colClass = '';
	// only define a default col-md- width if we don't have a col-xs- / col-sm- value which overrides it
	var sizeMd = props.sizeMd || props.size || (!props.sizeXs && !props.sizeMd ? 6 : undefined);

	if (props.sizeXs) {
		// NB: no col-xs- prefix
		// ref: https://getbootstrap.com/docs/4.3/layout/grid/
		colClass = 'col-' + props.sizeXs;
	}

	if (props.sizeSm) {
		colClass += ' col-sm-' + props.sizeSm;
	}

	if (sizeMd) {
		colClass += ' col-md-' + sizeMd;
	}

	if (props.sizeLg) {
		colClass += ' col-lg-' + props.sizeLg;
	}

	if (props.sizeXl) {
		colClass += ' col-xl-' + props.sizeXl;
	}

	if (props.noGutters) {
		colClass += ' no-gutters';
	}

	if (props.className) {
		colClass += ' ' + props.className;
	}

	return <div
		className={colClass}
		{...(omit(this.props, ['className', 'noGutters', 'children', '__canvas']))}
	>
		{props.children}
	</div>;

}

var sizeOptions = [
	{ name: '1/12', value: 1 },
	{ name: '2/12', value: 2 },
	{ name: '3/12 (25%)', value: 3 },
	{ name: '4/12 (33%)', value: 4 },
	{ name: '5/12', value: 5 },
	{ name: '6/12 (50%)', value: 6 },
	{ name: '7/12', value: 7 },
	{ name: '8/12 (66%)', value: 8 },
	{ name: '9/12 (75%)', value: 9 },
	{ name: '10/12', value: 10 },
	{ name: '11/12', value: 11 },
	{ name: '12/12 (100%)', value: 12 }
];

Column.propTypes = {
	noGutters: 'boolean',
	size: sizeOptions,
	sizeXs: sizeOptions,
	sizeSm: sizeOptions,
	sizeMd: sizeOptions,
	sizeLg: sizeOptions,
	sizeXl: sizeOptions,
	children: true
};

Column.icon = 'columns';
