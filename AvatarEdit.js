import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';
import Loading from 'UI/Loading';
import Spacer from 'UI/Spacer';
import Uploader from 'UI/Uploader';
import Input from 'UI/Input';

export default class AvatarEdit extends React.Component {
	
    setAvatar(avatarRef){
        this.setState({updating: true, confirmDelete: false, next: null});
		webRequest('user/' + global.app.state.user.id, {avatarRef}).then(response => {
			global.app.setState({user: response.json});
            this.setState({updating: false});
        }).catch(e=>{
            console.error(e);
            this.setState({updating: false});
        });
    }
    
	render(){
		var avatarRef;
		var {name} = this.props;
		if(name){
			avatarRef = this.state.avatarRef || this.props.value || this.props.defaultValue;
		}else{
			var { user } = global.app.state;
			if(!user || !user.id){
				return null;
			}
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
									<button type="button" className="btn btn-success" style={{marginRight: '20px'}} onClick={() => this.setAvatar(next)}>
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
										<a href={getRef(avatarRef, {url: true})} target='_blank'>
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
															this.setAvatar(null);
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
								'No avatar yet'
							)
						)
					}
                </div> 
                <Spacer />
				{/* NB: 
				 * ID necessary to trigger rendering of label, which is where the CSS magic happens 
				 * label prop ensures any label supplied is passed along
				 * maxSize prop causes "Max file size: xx" when max bytes supplied (e.g. 1024 = 1KB)
				 */}
				<Uploader label={this.props.label} id={this.props.id} maxSize={this.props.maxSize} onUploaded={info => {
					console.log(info);
					if(this.props.name){
						this.setState({
							avatarRef: info.uploadRef
						});
					}else{
						this.setState({
							next: info.uploadRef
						});
					}
                }}/>
            </div>
        );
		
	}
	
}
