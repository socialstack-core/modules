import Modal from 'UI/Modal';
import Loop from 'UI/Loop';

var __moduleGroups = null;

export default class ModuleSelector extends React.Component {

    constructor(props){
        super(props);

        this.state = {

        };
    }

    collectModules(){
		// __mm is the superglobal used by socialstack 
		// to hold all available modules.
		var sets = {standard:{}, renderer: {}};
		__moduleGroups={};
		
		for(var modName in __mm){
			// Attempt to get React propTypes.
			var module = require(modName).default;
			
			if(!module){
				continue;
			}
			
			var props = module.propTypes;
			
			if(!props){
				// This module doesn't have a public interface.
				continue;
			}

			if(this.props.groups && this.props.groups != "*") {
				// This means we need to make sure the component is within our valid groups.
				if(!module.groups || module.groups != this.props.groups){
					continue;
				}
			}
			
			// modName is e.g. UI/Thing
			
			// Remove the filename, and get the super group:
			var nameParts = modName.split('/');			
			var publicName = nameParts.join('/');
			var name = nameParts.pop();
			
			if(nameParts[0] == 'UI'){
				nameParts.shift();
			}
			
			var group = nameParts.join(' > ');
			var setsToJoin = null;
			
			if(module.rendererPropTypes){
				// Both sets
				setsToJoin = [sets.standard, sets.renderer];
			}else{
				// 1 set
				setsToJoin=[sets[module.moduleSet || 'standard']];
			}
			
			for(var i=0;i<setsToJoin.length;i++){
				var set = setsToJoin[i];
				if(!set[group]){
					group = set[group] = {name: group, modules: []};
				}else{
					group = set[group];
				}
				
				group.modules.push({
					name,
					publicName,
					props: set == sets.renderer && module.rendererPropTypes ? module.rendererPropTypes : props || {},
					moduleClass: module
				});
			}
		}
		
		for(var setName in sets){
			var set = sets[setName];
			
			var moduleGroups = [];
			if(set[""]){
				// UI group first always:
				moduleGroups.push(set[""]);
				delete set[""];
			}
			
			for(var gName in set){
				moduleGroups.push(set[gName]);
			}
			
			__moduleGroups[setName] = moduleGroups;
		}
		
	}

    render(){
		
		var set = null;
		
		if(this.props.selectOpenFor){
			if(!__moduleGroups){
				this.collectModules();
			}
			set = this.props.moduleSet ? __moduleGroups[this.props.moduleSet] : __moduleGroups.standard;
		}
		
		return (<Modal
			className={"module-select-modal"}
			buttons={[
				{
					label: "Close",
					onClick: this.props.closeModal
				}
			]}
			isLarge
			title={"Add something to your content"}
			onClose={this.props.closeModal}
			visible={this.props.selectOpenFor}
		>
			{set ? set.map(group => {
				return <div className="module-group">
					<h6>{group.name}</h6>
					<Loop asCols over={group.modules} size={4}>
						{module => {
							return <div className="module-tile" onClick={() => {
									this.props.onSelected && this.props.onSelected(module)	
								}}>
								<div>
									{<i className={"fa fa-" + (module.moduleClass.icon || "puzzle-piece")} />}
								</div>
								{module.name}
							</div>;
						}}
					</Loop>
				</div>;
				
			}) : null}
		</Modal>);
	}
}