import Loading from 'UI/Loading';
import Input from 'UI/Input';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';

/**
 * Used to select a user.
 * Displays their username, firstName + lastName (if the user object has this) and avatar.
 * Can alternatively provide a props.onDisplay(user, isSuggestion) method which renders the user rows as you wish.
 */
export default class UserInput extends React.Component {
	constructor(props){
		super(props);
		this.state = {
			loading: false,
			searchMode: true,
			users: null
		};
	}
	
    componentDidMount(){
        var user = this.props.user;
        if(user){
            this.setState({
                user,
                searchMode: false
            });
        }else{
			var id = this.props.value || this.props.defaultValue;
			
			if(!id){
				return;
			}
			
			webRequest('user/' + id).then(response => {
				
				if(response && response.json){
					
					this.setState({
						user: response.json,
						searchMode: false
					});
					
				}
				
			});
		}
    }
    
	selectUser(user){
		this.setState({
			users: null,
			searchMode: false,
			user,
			id: user ? user.id : null
		});
		
		this.props.onChange && this.props.onChange(user);
	}
	
	search(query){
		this.setState({loading: true});
		
		var fields = [
			// {Username: {startsWith: query}},
			{FirstName: {startsWith: query}},
			// {LastName: {startsWith: query}},
			{Email: {startsWith: query}}
		];
		
		webRequest('user/list', {where: fields}).then(response => {
			var results = response.json.results;
			this.setState({loading: false, users: results});
		});
	}
	
	avatar(user) {
		if(user.avatarRef === undefined){
			return '';
		}
		
		return getRef(user.avatarRef);
	}
	
	display(user, isSuggestion){
		if(this.props.onDisplay){
			return this.props.onDisplay(user, isSuggestion);
		} 
		
		// Try a variety of common fields.
		// What's actually available will depend on the site.
		if(user.firstName){
			return [this.avatar(user), user.firstName + (user.lastName ? ' ' + user.lastName : '')];
		}
		
		if(user.username){
			return [this.avatar(user), user.username];
		}
		
		if(user.email){
			return [this.avatar(user), user.email];
		}
		
		return [this.avatar(user), "#" + user.id];
	}
	
	renderInput(){
		return [
			this.state.searchMode ? (
				<div>
					<input autoComplete="false" className="form-control" placeholder="Find a user.." type="text" name={"__user_search_" + (this.props.name || "user")} onKeyUp={(e) => {
						this.search(e.target.value);
					}}/>
					{this.state.users && (
						<div className="suggestions">
							{this.state.users.length ? (
								this.state.users.map((user, i) => (
									<div key={i} onClick={() => this.selectUser(user)} className="suggestion">
										{this.display(user, true)}
									</div>
								))
							) : (
								<div>
									No users found
								</div>
							)}
						</div>
					)}
				</div>
			) : (
				<div>
					{this.state.user && this.display(this.state.user, false)} <div style={{marginLeft: '15px'}} className="btn btn-secondary" onMouseDown={() => this.setState({searchMode: true})}>Change</div>
				</div>
			),
			<input type="hidden" value={this.state.id || this.props.value || this.props.defaultValue} name={this.props.name || "user"} />	
		];
	}
	
	render() {
		return <Input {...this.props} type={() => this.renderInput()}/>;
	}
}