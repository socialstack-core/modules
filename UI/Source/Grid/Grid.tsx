import Row from 'UI/Row';

/**
 * Props for the Grid component.
 */
interface GridProps extends React.HTMLAttributes<HTMLSpanElement> {
	/**
		Set this to true if you don't want gutters on your grid.
	*/
	noGutters?: boolean;

	/**
	 * The number of rows in the grid.
	 */
	rows?: int;

	/**
	 * The number of columns in the grid.
	 */
	columns?: int;

	/**
	 * Grid layout mode. Only auto is permitted at the moment.
	 */
	layout?: 'auto' | 'manual';

	/**
	 * The cells of the grid.
	 */
	cells: React.ReactNode[]
}


const GRID_DEFAULT_ROWS = 1;
const GRID_DEFAULT_COLUMNS = 2;
const GRID_DEFAULT_LAYOUT = 'auto';


/**
 * Used to define a grid based layout.
 * @param props
 * @returns
 */
const Grid: React.FC<GridProps> = (props) => {
	var rows = props.rows || GRID_DEFAULT_ROWS;
	var columns = props.columns || GRID_DEFAULT_COLUMNS;
	var layout = props.layout || GRID_DEFAULT_LAYOUT;
	
	if (rows <= 0 || columns <= 0){
		return null;
	}
	
	if (columns > 12){
		columns = 12;
	}

	var rowSet = [];
	var i = 0;

	if (layout == 'manual') {
		// TODO
	} else {
		// auto layout
		for (var r = 0; r < rows; r++) {
			var colSet = [];

			for (var c = 0; c < columns; c++) {
				var colContent = props.cells[(i++)];

				if (colContent === undefined) {
					colContent = null; // react requires null if this happens
				}

				colSet.push(<>
					<div className="col">
						{colContent}
					</div>
				</>);
			}

			rowSet.push(<>
				<Row noGutters={props.noGutters ? true : undefined}>
					{colSet}
				</Row>
			</>);
		}

    }

	return rowSet;
}

/*
Grid.propTypes = {
	rows: 'int',
	//columns: 'int',
	columns: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
	noGutters: 'boolean',
	/*
	layout: [
		{ name: `Automatic`, value: 'auto' },
		{ name: `Manual`, value: 'manual' }
	]
	 *
};

Grid.defaultProps = {
	rows: GRID_DEFAULT_ROWS,
	columns: GRID_DEFAULT_COLUMNS,
	noGutters: GRID_DEFAULT_NOGUTTERS,
	//layout: GRID_DEFAULT_LAYOUT
}

Grid.onEditorUpdate = (node, rte) => {
	if(!node || !node.props){
		return;
	}
	
	var w = parseInt(node.props.columns) || GRID_DEFAULT_COLUMNS;
	var h = parseInt(node.props.rows) || GRID_DEFAULT_ROWS;
	
	var cellCount = w * h;

	// Ensure there are cellCount roots:
	if(!node.roots){
		node.roots = {};
	}
	
	for(var i=0; i < cellCount; i++){
		if(!node.roots['c' + i]){
			rte.addEmptyRoot(node, 'c' + i);
		}
	}
};

Grid.priority = true;
*/