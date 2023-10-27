import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import Input from 'UI/Input';
import { collectModules } from './Utils';
import { useState, useEffect } from 'react'; 

var __moduleGroups = null;

export default function ModuleSelector(props) {
	const { selectOpenFor, moduleSet } = props;
	var [allModules, setAllModules] = useState(null);
	var [filteredModules, setFilteredModules] = useState(null);
	var [sortOrder, setSortOrder] = useState('alpha');

	useEffect(() => {

		if (selectOpenFor && !allModules) {

			if (!__moduleGroups) {
				__moduleGroups = collectModules();
			}

			let modules = moduleSet ? __moduleGroups[moduleSet] : __moduleGroups.standard;
			setAllModules(modules);
			setFilteredModules(updateSortOrder(modules));
		}

	});

	useEffect(() => {

		if (filteredModules) {
			setFilteredModules(updateSortOrder(filteredModules));
        }

	}, [sortOrder]);

	function updateSortOrder(modules) {
		return modules.map(group => {
			var sortedModules = sortOrder == 'popularity' ?
				group.modules.sort((a, b) => (b.priority ? 1 : 0) - (a.priority ? 1 : 0) || a.name.localeCompare(b.name)) :
				group.modules.sort((a, b) => a.name.localeCompare(b.name));

			return {
				'name': group.name,
				'modules': sortedModules
			};
        });
    }

	function updateFilter(event) {
		var filterText = event.target.value;

		// cleared filter?
		if (!filterText || !filterText.length) {
			setFilteredModules(updateSortOrder(allModules));
			return;
		}

		filterText = filterText.toLowerCase();

		var filtered = allModules.map(group => {
			var matchingModules = group.modules.filter(module => {
				return module.name.toLowerCase().includes(filterText);
			});

			return {
				'name': group.name,
				'modules': matchingModules
			};

		});

		// remove empty groups
		filtered = filtered.filter(group => {
			return group.modules.length
		});

		setFilteredModules(updateSortOrder(filtered));
	}

	function updateSort(event) {
		setSortOrder(event.target.value);
    }

	function onCloseModal() {
		// clear filter
		setFilteredModules(updateSortOrder(allModules));

		if (props.closeModal instanceof Function) {
			props.closeModal();
		}

	}

	return <>
		<Modal
			className={"module-select-modal"}
			buttons={[
				{
					label: `Close`,
					onClick: onCloseModal
				}
			]}
			isLarge
			title={`Add something to your content`}
			onClose={onCloseModal}
			visible={selectOpenFor}
		>
			<div className="module-groups-filters">
				<div className="row">
					<div className="col-12 col-lg-8">
						<Input type="search" autoFocus noWrapper onInput={updateFilter}
							label={`Filter by module name and / or associated keywords`} placeholder={`e.g. "Text", "accordion", etc.`} />
					</div>
					<div className="col-12 col-lg-4">
						<Input type="select" noWrapper onChange={updateSort} label={`Sort Order`}>
							<option value={'alpha'}>{`Alphabetically`}</option>
							<option value={'popularity'}>{`By popularity`}</option>
						</Input>
					</div>
				</div>
			</div>
			<div className="module-groups-wrapper">
				{filteredModules ? filteredModules.map(group => {

					/*
					if(this.props.groups && this.props.groups != "*") {
						// This means we need to make sure we don't display a module unless it is within the specified group(s).
					}
					*/

					return <div className="module-group">
						<h6 className="module-group__name">
							{group.name || `Common Modules`}
						</h6>
						<div className="module-group__internal">
							<Loop raw over={group.modules}>
								{module => {
									let wrappedModuleName = module.name.match(/[A-Z][a-z]*/g);

									return <>
										<button type="button" className="btn module-tile" onClick={() => {

											if (props.onSelected instanceof Function) {
												props.onSelected(module);
											}

											onCloseModal();
										}}>
											{module.priority && <>
												<i className="fa fa-star module-tile__popular" title={`Popular`}></i>
											</>}
											<div>
												{<i className={"fa fa-" + (module.moduleClass.icon || "puzzle-piece")} />}
											</div>

											{/* display module name with preferred workbreak markers between each word */}
											{wrappedModuleName.map(word => {
												return <>
													{word}
													<wbr />
												</>;
											})}
										</button>
									</>;
								}}
							</Loop>
						</div>
					</div>;

				}) : null}
			</div>
		</Modal>
	</>;

}
