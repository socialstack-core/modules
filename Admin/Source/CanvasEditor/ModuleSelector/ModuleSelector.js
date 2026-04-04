import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import Input from 'UI/Input';
import Loading from 'UI/Loading';
import { collectModules, groupByDirectory } from './Utils';
import { useState, useEffect } from 'react';

function formatTitle(name) {
	return name.replace(/([a-z])([A-Z])/g, '$1 $2');
} 

export default function ModuleSelector(props) {
	const { selectOpenFor, componentGroups, onClose, onSelected } = props;
	var [componentSet, setComponentSet] = useState(null);
	var [filter, setFilter] = useState(null);
	var [sortOrder, setSortOrder] = useState('alpha');
	
	useEffect(() => {
		collectModules(componentGroups).then(compSet => {
			setComponentSet(compSet);
		});
	}, [props.componentGroups]);
	
	function updateSort(event) {
		setSortOrder(event.target.value);
    }
	
	const renderModalContent = () => {
		
		// Filter and sort the modules now (they are already filtered by componentGroups)
		var filteredModules = componentSet.modules;
		
		if(filter){
			filteredModules = filteredModules.filter(mod => {
				return mod.publicName.replace(/\s+/g, '').toLowerCase().indexOf(filter) != -1;
			});
		}
		
		if(sortOrder == 'alpha'){
			filteredModules = filteredModules.sort((a, b) => (a.publicName > b.publicName) ? 1 : ((b.publicName > a.publicName) ? -1 : 0));
		}
		
		// Group them by directory:
		var dirGroups = groupByDirectory(filteredModules);
		
		return <>
			<div className="module-groups-filters row">
				<div className="col-6">
					<Input type="search" autoFocus noWrapper onInput={el => {
						var filterText = el.target.value.replace(/\s+/g, '').toLowerCase();
						setFilter(filterText);
					}}
						placeholder={`Search components...`} />
				</div>
				<div className="col-6">
					<Input type="select" noWrapper onChange={updateSort}>
						<option value={'alpha'}>{`Alphabetical`}</option>
						<option value={'popularity'}>{`Popularity`}</option>
					</Input>
				</div>
			</div>
			<div className="module-groups-wrapper">
				{dirGroups.map(dir => {

					return <div className="module-group">
						<h6 className="module-group__name">
							{dir.name || `Common Modules`}
						</h6>
						<div className="module-group__internal">
							{dir.modules.map(module => {
								var icon = module.meta?.icon || 'fa fa-puzzle-piece';
								var description = module.meta?.description || '';
								return <button type="button" className="btn module-tile" onClick={() => {
									onSelected && onSelected(module);
									onClose && onClose();
								}}>
									{module.priority && <i className="fa fa-star module-tile__popular" title={`Popular`}></i>}
									<div className="module-tile__icon">
										<i className={icon} />
									</div>
									<div className="module-tile__content">
										<div className="module-tile__title">{formatTitle(module.name)}</div>
										<div className="module-tile__subtitle">{description}</div>
									</div>
								</button>;
							})}
						</div>
					</div>;
				})}
			</div>
		</>;
	};
	
	return <>
		<Modal
			className={"module-select-modal"}
			buttons={[
				{
					label: `Close`,
					onClick: onClose
				}
			]}
			isLarge
			title={`Add Component`}
			onClose={onClose}
			visible={selectOpenFor}
		>
			{!componentSet ? <Loading /> : renderModalContent()}
		</Modal>
	</>;

}
