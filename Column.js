import omit from 'UI/Functions/Omit';

/**
 * A 12 segment responsive column. Usually used within a <Row>.
 */

export default class Column extends React.Component {

	render() {
		var props = this.props;
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

}

Column.propTypes = {
	noGutters: 'boolean',
	size: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
	children: true
};

Column.icon = 'columns';
