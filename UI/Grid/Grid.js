import Row from 'UI/Row';
import Column from 'UI/Column';

export default function Grid(props) {
	
	var rows = props.rows || 1;
	var columns = props.columns || 3;
	
	if(rows <= 0 || columns <= 0){
		return null;
	}
	
	if(columns > 12){
		columns = 12;
	}
	
	var colSize = Math.floor(12 / columns);
	
	var rowSet = [];
	var i = 0;
	
	for(var r=0;r<rows;r++){
		var colSet = [];
		
		for(var c=0;c<columns;c++){
			var colContent = props['c' + (i++)];
			
			if(colContent === undefined){
				colContent = null; // react requires null if this happens
			}
			
			colSet.push(<Column size={colSize}>{colContent}</Column>);
		}
		
		rowSet.push(<Row>{colSet}</Row>);
	}
	
	return rowSet;
}

Grid.propTypes = {
	rows: 'int',
	columns: 'int'
};

Grid.defaultProps = {
	columns: 3,
	rows: 1
}

Grid.onEditorUpdate = (node, rte) => {
	if(!node || !node.props){
		return;
	}
	
	var w = parseInt(node.props.columns) || 1;
	var h = parseInt(node.props.rows) || 3;
	
	var cellCount = w * h;
	
	// Ensure there are cellCount roots:
	if(!node.roots){
		node.roots = {};
	}
	
	for(var i=0;i<cellCount;i++){
		if(!node.roots['c' + i]){
			rte.addEmptyRoot(node, 'c' + i);
		}
	}
};
