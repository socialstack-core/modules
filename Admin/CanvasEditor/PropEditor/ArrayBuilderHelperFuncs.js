import Input from 'UI/Input';

const selectPlaceholder = 'select';

function getDefaultValue(index, defaultValues) {
    if (defaultValues && defaultValues[index]) {
        return defaultValues[index];
    }

    return null;
}

function parseNumberValues(groupData) {
    var output = groupData;

    for (let i = 0; i < groupData.length; i++) {
        var keys = Object.keys(output[i]);

        for (let j = 0; j < keys.length; j++) {
            var key = keys[j];

            if (output[i][key] !== '' && isFinite(output[i][key])) {
                output[i][key] = parseFloat(output[i][key]);
            }
        }
    }

    return output;
}

function createDefaultRowData(types, props, defaultValues) {
    var rowCount = defaultValues?.length ?? 1;

    var defaultValueRowData = [];
    for (let i = 0; i < rowCount; i++) {
        var defaultVal = Array.isArray(defaultValues) ? getDefaultValue(i, defaultValues) : defaultValues;
        let row = createArrayRow(types, props, defaultVal);
        defaultValueRowData.push(row);
    }

    return defaultValueRowData;
}

function updateInputRefs(rowData) {
    for (let i = 0; i < rowData.length; i++) {
        for (let j = 0; j < rowData[i].row.length; j++) {
            var inputRef = rowData[i].row[j].ref.current.inputRef;
            if (inputRef) {
                var val = rowData[i].values[Object.keys(rowData[i].values)[j]];
                inputRef.value = val;
            }
        }
    }
}

function createArrayRow(types, props, defaultValues) {
    // Row init
    var row = <div className='array-row'></div>;
    row.props.children = [];

    var typeKeys = Object.keys(types);

    // Create an Input component for each type in types and add to row
    var refs = [];
    for (let i = 0; i < typeKeys.length; i++) {
        var ref = React.createRef();
        refs.push(ref);
        var key = typeKeys[i];
        var label = key;
        var defaultVal = defaultValues ? defaultValues[key] : '';

        var options = null;
        var type = types[key];
        if (Array.isArray(types[key])) {
            options = types[key];
            type = 'select';
        }

        var inputEle = <Input ref={ref} type={type} label={label} defaultValue={defaultVal} {...props}>
            {options && [
                <option>{selectPlaceholder}</option>,
                ...options.map(optionVal => {
                    return <option value={optionVal}>{optionVal}</option>;
                }),
            ]}
        </Input>;

        row.props.children.push(inputEle);
    }

    // Create a JSX element array for internal use
    var rowArr = [];
    for (let i = 0; i < row.props.children.length; i++) {
        var jsxEle = row.props.children[i];
        rowArr.push(jsxEle);
    }

    return { display: row, row: rowArr, refs: refs, values: defaultValues };
}

function getValues(rowData, useInternalValuesIfSet = false) {
    var extractedVals = [];
    var rowDataIsValid = Array.isArray(rowData) && rowData.length > 0 && rowData[0].values;

    if (useInternalValuesIfSet && rowDataIsValid) {
        var keys = Object.keys(rowData[0].values);

        for (let i = 0; i < rowData.length; i++) {
            var values = rowData[i].values;
            var extractedRow = {};

            for (let j = 0; j < keys.length; j++) {
                extractedRow[keys[j]] = values ? values[keys[j]] : '';
            }

            extractedVals.push(extractedRow);
        }
    } else {
        extractedVals = rowData.map((dataRow) => {
            var valueRow = {};

            for (let i = 0; i < dataRow.row.length; i++) {
                var ele = dataRow.row[i];
                var input = ele?.ref?.current?.base?.lastChild;
                var value = (input) ? input.value : '';

                if (input.type === 'select-one') {
                    var selected = input.selectedOptions;

                    if (selected && selected.length > 0) {
                        value = (selected[0].text !== selectPlaceholder) ? selected[0].text : '';
                    }
                }

                valueRow[ele.props.label] = value;
            }

            return valueRow;
        });
    }

    return parseNumberValues(extractedVals);
}

export default {
    createDefaultRowData,
    updateInputRefs,
    createArrayRow,
    getValues,
}