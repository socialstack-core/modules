import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';
import Loading from 'UI/Loading';
import Spacer from 'UI/Spacer';
import Uploader from 'UI/Uploader';

export default class AvatarEdit extends React.Component {
	
    setAvatar(avatarRef){
        this.setState({updating: true, next: null});
		webRequest('user/' + global.app.state.user.id, {avatarRef}).then(response => {
			global.app.setState({user: response.json});
            this.setState({updating: false});
        }).catch(e=>{
            console.error(e);
            this.setState({updating: false});
        });
    }
    
	render(){
		var { user } = global.app.state;
		if(!user || !user.id){
            return null;
        }
        
		var {updating, confirmDelete, next} = this.state;
		
        if(updating){
            return (
                <div>
                    <div>
                        <Loading />
                    </div>
                </div>
            );  
        }
        
        return (
            <div className="avatarEdit">
                <div>
					{
						next ? (
							<div>
								<a href={getRef(next, {url: true})} target='_blank'>
									{getRef(next, {size: 128})}
								</a>
								<div style={{margin: '20px'}}>
									<p>
										Are you sure you want to set this as your avatar?
									</p>
									<div className="btn btn-success" style={{marginRight: '20px'}} onClick={() => this.setAvatar(next)}>
										Yes
									</div>
									<div className="btn btn-primary" onClick={() => this.setState({next: null})}>
										Cancel
									</div>
								</div>
							</div>
						) : (
							user.avatarRef && user.avatarRef.length ? (
								<div>
									{
										<a href={getRef(user.avatarRef, {url: true})} target='_blank'>
											{getRef(user.avatarRef, {size: 128})}
										</a>
									}
									{
										confirmDelete ? (
											<div style={{margin: '20px'}}>
												<p>
													Are you sure you want to remove this?
												</p>
												<div className="btn btn-danger" style={{marginRight: '20px'}} onClick={() => this.setAvatar(null)}>
													Yes - Remove it
												</div>
													<div className="btn btn-primary" onMouseDown={() => this.setState({confirmDelete: false})}>
													Cancel
												</div>
											</div>
										) : (
											<div className="btn btn-danger" style={{marginLeft: '20px'}} onClick={() => this.setState({confirmDelete: true})}>
												Remove
											</div>
										)
									}
								</div>
							) : (
								'No avatar yet'
							)
						)
					}
                </div> 
                <Spacer />
                <Uploader onUploaded={info => {
					console.log(info);
					this.setState({
						next: info.uploadRef
					});
                }}/>
            </div>
        );
		
	}
	
}
