import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import {collectModules} from './Utils';

var __moduleGroups = null;

export default class ModuleSelector extends React.Component {

    constructor(props){
        super(props);

        this.state = {

        };
    }
	
    render(){
		
		var set = null;
		
		if(this.props.selectOpenFor){
			if(!__moduleGroups){
				__moduleGroups = collectModules();
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
				
				/*
				if(this.props.groups && this.props.groups != "*") {
					// This means we need to make sure we don't display a module unless it is within the specified group(s).
				}
				*/
				
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