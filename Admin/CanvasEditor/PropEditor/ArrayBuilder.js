import { useState, useEffect } from 'react';
import helper from './ArrayBuilderHelperFuncs';

const showMaxReachedTimeMs = 3000;

export default function ArrayBuilder(props){
	const [rowData, setRowData] = useState([]);
	const [needsRebuild, setNeedsRebuild] = useState(false);
	const [hasRebuilt, setHasRebuilt] = useState(false);
	const [initialValuesLoaded, setInitialValuesLoaded] = useState(false);
	const [showMaxReachedPrompt, setShowMaxReachedPrompt] = useState(false)

	const inputProps = props.props;
	const types = inputProps.customMeta.fields;
	const maxRows = inputProps.customMeta.maxRows;
	const placeholderValues = inputProps.placeholder;

	function addArrayRow(row) {
		var newRowData = rowData.slice();
		newRowData.push(row);

		setRowData(newRowData);
		setNeedsRebuild(true);
	}

	function removeArrayRow(index, array) {
		var newData = array.slice();
		newData.splice(index, 1);

		setRowData(newData);
		setNeedsRebuild(true);
	}

	function removeRowButton(i, rowData) {
		return <button className='remove-row-btn'
			onClick={() => { removeArrayRow(i, rowData); }}
		>
			<i className="fa fa-minus" />
		</button>;
	}

	function rebuildRows(rowData) {
		var newData = [];

		// Create a copy of each row
		for (let i = 0; i < rowData.length; i++) {
			var rowValues = rowData[i].values;
			newData.push(helper.createArrayRow(types, props, rowValues));
		}

		// Create a remove button for each row
		for (let i = 0; i < newData.length; i++) {
			var removeFromArrayBtn = removeRowButton(i, newData);

			if (newData[i].display.props.children[newData[i].display.props.children.length-1].type === 'button') {
				newData[i].display.props.children.splice(newData[i].display.props.children.length-1, 1);
			}
			newData[i].display.props.children.push(removeFromArrayBtn);
		}

		return newData;
	}

	function onChangeFunc(e, rowDat = rowData, useExtracted = false) {
		var groupData = helper.getValues(rowDat, useExtracted);

		for (let i = 0; i < rowDat.length; i++) {
			var refs = rowDat[i].refs;

			for (let j = 0; j < refs.length; j++) {
				if (refs[j]?.current?.base) {
					var inputGroup = refs[j].current.base;
					var inputEle = inputGroup.lastChild;

					inputEle.groupData = groupData;
					rowDat[i].values = inputEle.groupData[i];

					// On the last iteration, fire an onchange event
					if (i === rowDat.length-1 && j === refs.length-1) {
						var event = new Event('change');
						inputEle.dispatchEvent(event);
					}
				}
			}
		}
	}

	function applyOnChangeFuncToInputs(rowData) {
		for (let i = 0; i < rowData.length; i++) {
			var dataRow = rowData[i];
			var row = dataRow.row;
	
			for (let j = 0; j < row.length; j++) {
				var input = row[j];
				input.props.onInput = onChangeFunc;
			}
		}
	}

	useEffect(() => {
		if (needsRebuild) {
			setRowData(rebuildRows(rowData));
			setNeedsRebuild(false);
			setHasRebuilt(true);
		} else if (hasRebuilt) {
			if (!initialValuesLoaded) {
				// Don't call onChangeFunc on init
				setInitialValuesLoaded(true);
			} else {
				onChangeFunc(null, rowData, true);

				// Update the inputRef values so the latest value is always shown
				helper.updateInputRefs(rowData);
			}
			setHasRebuilt(false);
		}
	});

	if (types && rowData.length === 0) {
		var initialValues = null;
		if (!initialValuesLoaded) {
			var initialValues = inputProps.defaultValue ?? placeholderValues;
		}

		var defaultValueRowData = helper.createDefaultRowData(types, props, initialValues);
		setRowData(defaultValueRowData);
		setNeedsRebuild(true);
	} else {
		applyOnChangeFuncToInputs(rowData);
	}

	var firstPlaceholderRow = (Array.isArray(placeholderValues) && placeholderValues.length > 0) ? placeholderValues[0] : placeholderValues;

	return (<div className='array-builder'>
        <div style={{position: 'relative', display: 'flex', flexDirection: 'column'}}>
            <div className='btn-msg red'>{showMaxReachedPrompt && "Max has been reached (" + maxRows + ")"}</div>
			<button className={'add-row-btn' + (showMaxReachedPrompt ? ' red' : '')}
                onClick={() => {
					if (!maxRows || rowData.length < maxRows) {
                    	addArrayRow(helper.createArrayRow(types, props, firstPlaceholderRow));
					} else {
						setShowMaxReachedPrompt(true);
						if (showMaxReachedTimeMs > 0) {
							setTimeout(() => { setShowMaxReachedPrompt(false); }, showMaxReachedTimeMs);
						}
					}
                }}
            >
                <i className="fa fa-plus" />
            </button>
            {rowData.map(dataRow => dataRow.display)}
        </div>
	</div>);
}
