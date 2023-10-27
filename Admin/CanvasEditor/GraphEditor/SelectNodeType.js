import Loop from 'UI/Loop';
import Modal from 'UI/Modal';
import {niceName, getNodeTypes} from './Utils';
import getAutoForm, {getAllContentTypes} from 'Admin/Functions/GetAutoForm';
import {collectModules} from '../ModuleSelector/Utils';



var availableNodeTypes = null;

export default function NodeTypeSelect(props){
	
	if(!availableNodeTypes){
		availableNodeTypes = getNodeTypes();
	}
	
	var nsLc = props.namespace ? props.namespace.toLowerCase() : null;
	
	// Filter by namespace:
	var nsModules = nsLc ? availableNodeTypes.filter(nt => nt.publicPath.toLowerCase().startsWith(nsLc)) : availableNodeTypes;
	
	return <Modal visible 
			isLarge
			title={`Add a node to your graph`}
			onClose={props.onClose} className={"module-select-modal"}>
			<div className="module-group">
			<Loop asCols over={nsModules} size={4}>
				{moduleAndName => {
					var nodeType = moduleAndName.module;
					var name = nodeType.name;
					
					return <div className="module-tile" onClick={() => {
							props.onSelected && props.onSelected({Type: nodeType, name})	
						}}>
						<div>
							{<i className={"fa fa-" + (nodeType.icon || "puzzle-piece")} />}
						</div>
						{niceName(name)}
					</div>;
				}}
			</Loop>
		</div>
	</Modal>
}
