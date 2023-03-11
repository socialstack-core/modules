import Row from 'UI/Row';

const GRID_DEFAULT_NOGUTTERS = false;
const GRID_DEFAULT_ROWS = 1;
const GRID_DEFAULT_COLUMNS = 2;
const GRID_DEFAULT_LAYOUT = 'auto';

export default function Grid(props) {
	var noGutters = (props.noGutters == undefined) ? GRID_DEFAULT_NOGUTTERS : props.noGutters;
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
				var colContent = props['c' + (i++)];

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
				<Row noGutters={noGutters ? true : undefined}>
					{colSet}
				</Row>
			</>);
		}

    }

	return rowSet;
}

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
	 */
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
