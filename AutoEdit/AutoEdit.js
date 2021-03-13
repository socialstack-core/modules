import Tile from 'Admin/Tile';
import Loop from 'UI/Loop';
import AutoForm from 'Admin/AutoForm';
import Default from 'Admin/Layouts/Default';
import omit from 'UI/Functions/Omit';


export default class AutoEdit extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		return (
			<Default>
				<AutoForm {...(omit(this.props, ['children']))} />
				{this.props.children}
			</Default>	
		);
	}
	
}

AutoEdit.propTypes = {
	children: true,
	id: 'string',
	endpoint: 'string',
	deletePage: 'string'
};