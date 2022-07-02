import getRef from 'UI/Functions/GetRef';


export default class Signpost extends React.Component{
	
	render(props) {
		
		let {
			username,
			avatarRef,
			id
		} = props.user;
		
		return <div className="user-signpost">
			<div className="user-image">
				{getRef(avatarRef)}
			</div>
			<div className="user-details">
				<div className="user-name">
					<a href={'/user/' + id + '/' + encodeURIComponent(username)}>
						{username}
					</a>
				</div>
			</div>
		</div>
		
	}
	
}