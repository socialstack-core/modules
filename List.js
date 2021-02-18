import Tile from 'Admin/Tile';
import AutoList from 'Admin/AutoList';
import Loop from 'UI/Loop';
import Default from 'Admin/Pages/Default';


export default class List extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		return (
			<Default>
				{!this.props.noCreate && 
					<Tile>
						<a href={'/en-admin/' + this.props.endpoint + '/add'}>
							<div className="btn btn-primary">Create</div>
						</a>
					</Tile>
				}
				<AutoList endpoint={this.props.endpoint} {...this.props} />
				{this.props.children}
			</Default>	
		);
	}
	
}

List.propTypes = {
	children: true
};