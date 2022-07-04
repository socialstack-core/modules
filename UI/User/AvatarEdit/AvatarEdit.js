import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';
import Loading from 'UI/Loading';
import Uploader from 'UI/Uploader';
import Input from 'UI/Input';
import {useSession, SessionConsumer} from 'UI/Session';

export default class AvatarEdit extends React.Component {

    setAvatar(avatarRef, session){
		const { setSession } = useSession();
        this.setState({updating: true, confirmDelete: false, next: null});
		webRequest('user/' + session.user.id, {avatarRef}).then(response => {
			setSession({...session, user: response.json})
            this.setState({updating: false});
        }).catch(e=>{
            console.error(e);
            this.setState({updating: false});
        });
    }

	render(){
		return <SessionConsumer>
			{session => this.renderIntl(session)}
		</SessionConsumer>
	}
    
	renderIntl(session){
		var { avatarRef } = this.state;
		var {name} = this.props;
		if(name){
			if(avatarRef === undefined){
				avatarRef = this.props.value || this.props.defaultValue;
			}
		}else{
			var { user } = session;
			if(!user || !user.id){
				return null;
			}
			avatarRef = user.avatarRef;
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
				{
					name && <Input type="hidden" name={name} value={avatarRef}/>
				}
				<div className="avatar-edit-internal">
					{
						next ? (
							<div>
								<a href={getRef(next, { url: true })} target='_blank' rel="noopener noreferrer">
									{getRef(next, {size: 128})}
								</a>
								<div style={{margin: '20px'}}>
									<p>
										Are you sure you want to set this as your avatar?
									</p>
									<button type="button" className="btn btn-success" style={{marginRight: '20px'}} onClick={() => this.setAvatar(next, session)}>
										Yes
									</button>
									<button type="button" className="btn btn-primary" onClick={() => this.setState({next: null})}>
										Cancel
									</button>
								</div>
							</div>
						) : (
							avatarRef && avatarRef.length ? (
								<div>
									{
										<a href={getRef(avatarRef, { url: true })} target='_blank' rel="noopener noreferrer">
											{getRef(avatarRef, {size: 128})}
										</a>
									}
									{
										confirmDelete ? (
											<div style={{margin: '20px'}}>
												<p>
													Are you sure you want to remove this?
												</p>
													<button type="button" className="btn btn-danger" style={{marginRight: '20px'}} onClick={() => {
														if(this.props.name){
															this.setState({
																confirmDelete: false,
																avatarRef: null
															});
														}else{
															this.setAvatar(null, session);
														}
													}}>
													Yes - Remove it
												</button>
													<button type="button" className="btn btn-primary" onMouseDown={() => this.setState({confirmDelete: false})}>
													Cancel
												</button>
											</div>
										) : (
											<button type="button" className="btn btn-danger" style={{marginLeft: '20px'}} onClick={() => this.setState({confirmDelete: true})}>
												Remove
											</button>
										)
									}
								</div>
							) : (
							this.props.noAvatarText
							)
						)
					}
                </div> 

				{/* NB: 
				 * ID necessary to trigger rendering of label, which is where the CSS magic happens 
				 * label prop ensures any label supplied is passed along
				 * maxSize prop causes "Max file size: xx" when max bytes supplied (e.g. 1024 = 1KB)
				 */}
				<Uploader label={this.props.label} id={this.props.id} maxSize={this.props.maxSize} onUploaded={info => {
					console.log(info);
					var ref = info.result.ref;
					
					if(this.props.name){
						this.setState({
							avatarRef: ref
						});
					}else{
						this.setState({
							next: ref
						});
					}
                }}/>
            </div>
        );
		
	}
	
}
