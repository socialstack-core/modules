import Input from 'UI/Input';
import Search from 'UI/Search';
import Link from 'UI/Link';
import Button from "UI/Button";

/**
 * Dropdown to select a piece of content.
 */
export default class ContentSelect extends React.Component {

	constructor(props) {
		super(props);

		this.state = {
			searchSelected: null,
			selected: this.getDefaultValue(props),
			api: null
		};
		this.load(props, true);

		this.handleFormReset = () => {
			setTimeout(() => {
				this.setState({ searchSelected: null });
			}, 0);
		};
	}

	componentDidMount() {
		if (this.input && this.input.form) {
			this.input.form.addEventListener('reset', this.handleFormReset);
		}
	}

	componentWillUnmount() {
		if (this.input && this.input.form) {
			this.input.form.removeEventListener('reset', this.handleFormReset);
		}
	}

	componentWillReceiveProps(props) {

		if (props.search == this.props.search && props.contentType == this.props.contentType) {
			var newValue = props.value || props.defaultValue;
			var existingValue = this.props.value || this.props.defaultValue;

			if (newValue == existingValue) {
				return;
			}
		}

		this.load(props);
	}

	getDefaultValue(props) {
		var value = null;

		if (props.defaultValue) {
			if (props.defaultValue.id) {
				value = props.defaultValue.id;
			} else {
				value = props.defaultValue;
			}
		}

		return value;
	}

	/**
	 * Returns the best display title for a content object.
	 * Falls back to "No identifiable title" if none found.
	 */
	getDisplayTitle(content, titleField, validFields) {
		if (!content) return 'No identifiable title';

		if (titleField && content[titleField]) {
			return content[titleField];
		}

		for (const field of validFields || []) {
			if (content[field]) {
				return content[field];
			}
		}

		return 'No identifiable title';
	}

	load(props, first) {
		var api = require('Api/' + props.contentType).default;

		// Store API for use in search endpoint
		if (!this.state.api) {
			this.setState({ api });
		}

		if (props.search) {
			var value = props.value || props.defaultValue;
			if (value) {
				api.load(value).then(response => {
					this.setState({
						searchSelected: response
					});
				});
			} else if (!first) {
				this.setState({
					searchSelected: null
				});
			}
		} else {
			api.listAll().then(response => {
				var all = response.results;
				if (!props.hideDefaultValue) {
					all.unshift(null);
				}
				var selected = this.state.selected ? this.state.selected : this.getDefaultValue(props);
				this.setState({ all, selected: selected });
			});
		}
	}

	onChange(e) {
		if (this.props.onChange) {
			this.props.onChange(e);
		}

		var value = null;

		if (e && e.target && e.target.value && e.target.value > 0) {
			value = e.target.value;
		}

		this.setState({ selected: value });
	}

	render() {
		const { contentType, _isAdminSearch, ...props } = this.props;

		if (this.props.search) {
			var { searchSelected } = this.state;
			var title = '';

			if (searchSelected) {
				if (this.props.onRender) {
					var rendered = this.props.onRender(searchSelected);
					if (rendered) {
						title = rendered;
					}
				}
				if (!title) {
					var titleField = this.props.titleField;
					title = titleField && titleField.length && searchSelected[titleField]
						? searchSelected[titleField]
						: searchSelected.title || searchSelected.firstName || searchSelected.username || searchSelected.name || searchSelected.url;
				}
			}

			var value = this.props.defaultValue || this.props.value;

			return (
				<div className="mb-3 content-select">
					{this.props.label && (
						<label className="form-label">{this.props.label}</label>
					)}
					<input
						type="hidden"
						ref={ele => {
							this.input = ele;
							if (ele != null) {
								ele.onGetValue = (v, input, e) => {
									if (input != this.input) {
										return v;
									}

									return this.state.searchSelected ? this.state.searchSelected.id : '';
								};
							}
						}}
						name={this.props.name}
					/>
					<Search
						name=""
						for={contentType}
						endpoint={this.state.api ? this.state.api.list : props.endpoint}
						field={this.props.titleField || this.props.search}
						limit={5}
						placeholder={'Search for a ' + contentType + '..'}
						inputClassName="ui-form-control"
						onRender={this.props?.onRender}
						onQuery={this.props?.onQuery}
						onFind={entry => {
							this.setState({
								searchSelected: entry
							});
							if (this.props.onChange) {
								this.props.onChange({ target: { value: entry ? entry.id : null, name: this.props.name } });
							}
						}}
					/>
					<div className="selected-content">
						{searchSelected ? (
							<div className="selected-content__value">
								<span>{title}</span>
								<Button 
									sm 
									variant="secondary" 
									onClick={() => {
										this.setState({ searchSelected: null });
										if (this.props.onChange) {
											this.props.onChange({ target: { value: null, name: this.props.name } });
										}
									}}
								>
									<i className="fal fa-times"></i> Remove
								</Button>
							</div>
						) : value ? 'Item #' + value : 'None selected'}
					</div>
				</div>
			);
		}

		var all = this.state.all;
		var titleField = this.props.titleField;
		var validFields = [];

		if (all) {
			if (!titleField) {
				var firstValidItem = all.find(arg => arg != null);

				if (firstValidItem) {
					var fieldNames = ['title', 'firstName', 'username', 'name', 'url', 'email'];
					fieldNames.forEach(field => {
						if (firstValidItem[field]) {
							validFields.push(field);
						}
					});

					if (validFields.length > 0) {
						titleField = validFields[0];
					}
				}
			}

			// sort by title
			if (titleField && titleField.length) {
				all = all.sort((a, b) => {
					if (a && b) {
						let titleA = this.getDisplayTitle(a, titleField, validFields);
						let titleB = this.getDisplayTitle(b, titleField, validFields);
						return titleA.localeCompare(titleB);
					}
					return -1;
				});

				// ensure "none" is first
				let noneIndex = all.findIndex(content => !content);
				if (noneIndex > -1) {
					all.splice(noneIndex, 1);
					all.unshift(null);
				}
			}
		}

		var noSelection = this.props.noSelection || `None`;
		var mobileNoSelection = this.props.mobileNoSelection || `None`;

		if (
			window.matchMedia('(max-width: 752px) and (pointer: coarse) and (orientation: portrait)').matches ||
			window.matchMedia('(max-height: 752px) and (pointer: coarse) and (orientation: landscape)').matches
		) {
			noSelection = mobileNoSelection;
		}

		return (
			<div className="content-select">
				<Input
					{...props}
					noWrapper
					type="select"
					defaultValue={this.state.selected ? this.state.selected : null}
					onChange={e => {
						this.onChange(e);
					}}
				>
					{all
						? all.map(content => {
							if (!content) {
								return <option value={'0'}>{noSelection}</option>;
							}
							var title = this.getDisplayTitle(content, titleField, validFields);
							return <option value={content.id}>{`${title} (#${content.id})`}</option>;
						})
						: []}
				</Input>

				{!_isAdminSearch && <footer className="content-select__footer">
					<Link variant="primary" outlined sm
						href={'/en-admin/' + contentType.toLowerCase() + '/add'}
						className="btn-content-select-action btn-add-content"
					>
						<i className="fal fa-fw fa-plus"></i> {`New ${contentType}`}
					</Link>

					{this.state.selected && (
						<Link variant="primary" outlined sm
							href={'/en-admin/' + contentType.toLowerCase() + '/' + this.state.selected}
							className="btn-content-select-action btn-edit-content"
						>
							<i className="fal fa-fw fa-edit"></i> {`Edit ${contentType}`}
						</Link>
					)}
				</footer>}
			</div>
		);
	}
}
