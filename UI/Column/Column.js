import omit from 'UI/Functions/Omit';

/**
 * A 12 segment responsive column. Usually used within a <Row>.
 */

export default function Column (props) {
	var colClass = '';

	// PropEditor did at one point allow <option>s such as "Pick a value" to be selected;
	// ensure value saved in canvas JSON is a valid number
	var propList = ['', 'Xs', 'Sm', 'Md', 'Lg', 'Xl'];

	propList.forEach(prop => {
		var sizeProp = 'size' + prop;
		var offsetProp = 'offset' + prop;

		if (isNaN(props[sizeProp])) {
			props[sizeProp] = undefined;
		}

		if (isNaN(props[offsetProp])) {
			props[offsetProp] = undefined;
		}

	});

	// only define a default col-md- width if we don't have a col-xs- / col-sm- value which overrides it
	var sizeMd = props.sizeMd || props.size || (!props.sizeXs && !props.sizeMd ? 6 : undefined);

	if (props.offset) {

		if (!props.offsetXs) {
			props.offsetXs = props.offset;
		}
		if (!props.offsetSm) {
			props.offsetSm = props.offset;
		}
		if (!props.offsetMd) {
			props.offsetMd = props.offset;
		}
		if (!props.offsetLg) {
			props.offsetLg = props.offset;
		}
		if (!props.offsetXl) {
			props.offsetXl = props.offset;
		}

	}

	// size
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

	// offset
	if (props.offsetXs) {
		colClass += ' offset-' + props.offsetXs;
	}

	if (props.offsetSm) {
		colClass += ' offset-sm-' + props.offsetSm;
	}

	if (props.offsetMd) {
		colClass += ' offset-md-' + props.offsetMd;
	}

	if (props.offsetLg) {
		colClass += ' offset-lg-' + props.offsetLg;
	}

	if (props.offsetXl) {
		colClass += ' offset-xl-' + props.offsetXl;
	}

	// margin
	if (props.margin) {
		colClass += ' ' + props.margin;
	}

	if (props.noGutters) {
		colClass += ' gx-0';
	}

	if (props.className) {
		colClass += ' ' + props.className;
	}

	if (props.customClass) {
		colClass += ' ' + props.customClass;
	}

	return <div
		className={colClass} 
		{...(omit(props, ['className', 'noGutters', 'customClass']))}
	>
		{props.children}
	</div>;

}

var sizeOptions = [
	{ name: 'Auto', value: 'auto' },
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

var offsetOptions = sizeOptions.slice(1);
offsetOptions.splice(0, 0, { name: 'No offset', value: 0 });

var marginOptions = [
	{ name: 'None', value: '' },
	{ name: 'Move sibling columns right', value: 'ml-auto ms-auto' },
	{ name: 'Move sibling columns left', value: 'mr-auto me-auto' },
	{ name: 'Move sibling columns left/right', value: 'mx-auto' }
];

Column.propTypes = {
	noGutters: 'boolean', // remove?  pretty certain this applies to the parent row only
	size: sizeOptions,
	sizeXs: sizeOptions,
	sizeSm: sizeOptions,
	sizeMd: sizeOptions,
	sizeLg: sizeOptions,
	sizeXl: sizeOptions,
	offset: offsetOptions,
	offsetXs: offsetOptions,
	offsetSm: offsetOptions,
	offsetMd: offsetOptions,
	offsetLg: offsetOptions,
	offsetXl: offsetOptions,
	margin: marginOptions,
	children: true,
	customClass: 'string' // If you need to add a custom class for targeting children via CSS
};

Column.icon = 'columns';
