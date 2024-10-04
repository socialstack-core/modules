import { useState } from 'react';

/**
 * A general use "delimited string input"; primarily used for comma seperated values
 */

export default function MultiInput(props) {
	var delimiterFilter = (props.delimter || /[;,]/);

	const [items, setItems] = useState((props.value || props.defaultValue || '').split(delimiterFilter).filter(e => String(e).trim()));

	var delimiter = (props.delimter || ',');

	var fieldName = this.props.field;

	if (!fieldName) {
		fieldName = 'name';
	}

	var displayFieldName = this.props.displayField || fieldName;
	if(displayFieldName.length){
		displayFieldName = displayFieldName[0].toLowerCase() + displayFieldName.substring(1);
	}

	var atMax = false;
	
	if(this.props.max > 0){
		atMax = (items.length >= this.props.max);
	}

	const removeItem = (e,index) => {
		e.preventDefault();
		var clonedItems=[...items]
		clonedItems.splice(index , 1);
		setItems(clonedItems);
	}

	return (
		<div className="multiinput mb-3">
			{this.props.label && !this.props.hideLabel && (
				<label className="form-label">
					{this.props.label}
				</label>
			)}
			<ul className="multiinput__entries">
				{items.map((entry, i) => (
						<li key={i} className="multiinput__entry">
							<button className="btn btn-sm btn-outline-danger btn-entry-select-action btn-remove-entry" title={`Remove`}
								onClick={(e) => removeItem(e,i)}>
								<i className="fal fa-fw fa-times"></i> <span className="sr-only">{`Remove`}</span>
							</button>

							<div>
								{entry}
							</div>
						</li>
					))
				}
			</ul>
			<input type="hidden" name={this.props.name} ref={ref => {
				this.inputRef = ref;
				if (ref != null) {
					ref.onGetValue = (val, field,e) => {
						if (field != this.inputRef) {
							return val;
						}

						return items.join(delimiter);
					}
				}
			}} />
			<footer className="multiinput__footer">
				<div className="multiinput__search">
					{atMax ?
						<span className="multiinput__search-max">
							<i>{`Max of ${this.props.max} added`}</i>
						</span> 
						:
						<input id="input_values"
						autoComplete="off" className="form-control" 
						placeholder={this.props.placeholder || `Add a new entry, press {enter} to add`} 
						type="text"
						onKeyDown={(e) => {

							if (e.key == 'Enter' || e.key == ',') {
								if (e.target.value != '') {
									var clonedItems=[...items]
									clonedItems.push(e.target.value);
									setItems(clonedItems);
								}
								e.preventDefault();
							}
						}}
						onKeyUp={(e) => {
							if (e.key == 'Enter' || e.key == ',') {
								e.target.value = '';
								e.preventDefault();	
							}
						}} />
					}
				</div>
			</footer>
		</div>
	);
}
