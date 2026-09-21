/**
 * Props for the Column component.
 */
interface ColumnProps extends React.HTMLAttributes<HTMLDivElement> {

	/**
	 * Custom class name.
	 */
	className?: string;

	size?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeXs?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeSm?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeMd?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeLg?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeXl?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offset?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offsetXs?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offsetSm?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offsetMd?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offsetLg?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";

	/**
		An amount, in bootstrap grid columns, to offset your column by.
	*/
	offsetXl?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";

	/**
		Optionally add a specific type of margin to your column. See the bootstrap docs for what the available options do.
	*/
	margin?: "" | "ml-auto ms-auto" | "mr-auto me-auto" | "mx-auto";

	/**
		Set this to true if you don't want gutters on your column.
	*/
	noGutters?: boolean;

	children?: React.ReactNode | null

}

/**
 * A 12 segment responsive column. Usually used within a <Row>.
 */

const Column: React.FC<ColumnProps> = (props) => {
	var colClass = '';
	
	let {
		children,
		className,
		noGutters,
		sizeMd,
		size,
		sizeXl,
		sizeXs,
		sizeSm,
		sizeLg,
		offset,
		offsetXs,
		offsetSm,
		offsetMd,
		offsetLg,
		offsetXl,
		margin,
		customClass,
		...attribs
	} = props;
	
	// only define a default col-md- width if we don't have a col-xs- / col-sm- value which overrides it
	sizeMd = sizeMd || size || (!sizeXs && !sizeMd ? 6 : undefined);

	if (offset) {

		if (!offsetXs) {
			offsetXs = offset;
		}
		if (!offsetSm) {
			offsetSm = offset;
		}
		if (!offsetMd) {
			offsetMd = offset;
		}
		if (!offsetLg) {
			offsetLg = offset;
		}
		if (!offsetXl) {
			offsetXl = offset;
		}

	}

	// size
	if (sizeXs) {
		// NB: no col-xs- prefix
		// ref: https://getbootstrap.com/docs/4.3/layout/grid/
		colClass = 'col-' + sizeXs;
	}

	if (sizeSm) {
		colClass += ' col-sm-' + sizeSm;
	}

	if (sizeMd) {
		colClass += ' col-md-' + sizeMd;
	}

	if (sizeLg) {
		colClass += ' col-lg-' + sizeLg;
	}

	if (sizeXl) {
		colClass += ' col-xl-' + sizeXl;
	}

	// offset
	if (offsetXs) {
		colClass += ' offset-' + offsetXs;
	}

	if (offsetSm) {
		colClass += ' offset-sm-' + offsetSm;
	}

	if (offsetMd) {
		colClass += ' offset-md-' + offsetMd;
	}

	if (offsetLg) {
		colClass += ' offset-lg-' + offsetLg;
	}

	if (offsetXl) {
		colClass += ' offset-xl-' + offsetXl;
	}

	// margin
	if (margin) {
		colClass += ' ' + margin;
	}

	if (noGutters) {
		colClass += ' gx-0';
	}

	if (className) {
		colClass += ' ' + className;
	}
	
	if(customClass){
		colClass += ' ' + customClass;
	}
	
	return <div
		className={colClass} 
		{...attribs}
	>
		{children}
	</div>;

};

export default Column;

/*
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
*/
