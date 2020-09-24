import Tile from 'Admin/Tile';
import AutoList from 'Admin/AutoList';
import Loop from 'UI/Loop';
import Default from 'Admin/Pages/Default';


export default class List extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		var path = document.location.pathname;
		if(path[path.length-1] != '/'){
			path += '/';
		}
		
		return (
			<Default>
				{!this.props.noCreate && 
					<Tile>
						<a href={path + 'add'}>
							<div className="btn btn-primary">Create</div>
						</a>
					</Tile>
				}
				<AutoList endpoint={this.props.endpoint} path={path} {...this.props} />
				{this.props.children}
			</Default>	
		);
	}
	
}

List.propTypes = {
	children: true
};