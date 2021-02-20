import Input from "UI/Input";
import Form from "UI/Form";
import Alert from "UI/Alert";
import Row from "UI/Row";
import Modal from "UI/Modal";
import LoginForm from "UI/LoginForm";

export default class Add extends React.Component{
	constructor(props){
		super(props);
		this.state={};
	}

	close() {
		this.props.onClose && this.props.onClose();
	}
	
    render(){

        let {
			contentId,
			contentTypeId,
			visible,
			parentCommentId // (optional)
		} = this.props;

		if(!visible) {
			return;
		}

		console.log(global.app.state.user);

		if(!global.app.state.user) {
			return <div>
				<p>You must be logged in to leave a comment.</p>
				<Row className = "comment-buttons">
					<button onClick = {() => {this.setState({login: true})}} className = "btn btn-secondary">
						Login
					</button>
				</Row>

				<Modal
					visible = {this.state.login}
					onClose = {() => {this.setState({login: false});}}
					title = "Login"
				>
					<LoginForm noRedirect onLogin = {() => {this.setState({login:false})}}/>
				</Modal>

			</div>
		}
		
        return(
            <div className = "comment-add">
				<Form 
					action = "comment" 
					onFailure={
						response => {
							this.setState({failure: true, submitting: false});
						}
					}
					
					onSuccess={
						(response, values, e) => {
							this.setState({success: true, failure: false, submitting: false});
							this.props.onClose && this.props.onClose();
							e.target.reset();
						}
					}
					onValues = { values => {return {
						...values,
						contentId,
						contentTypeId,
						parentCommentId
					}}}
				>
                <Input placeholder="Leave us your thoughts!" type="textarea" name = "bodyJson" validate={["Required"]}/>
                <Row className = "comment-buttons">
					<Input type="submit" label="Comment"/>
					{this.props.onClose && <div className = "cancel-button form-group">
						<button className = "btn btn-danger" onClick = {() => {this.close();}}>Cancel</button>
					</div>}
				</Row>
				
				
				{
					this.state.failure && (
						<Alert type='error'>
							Something went wrong whilst trying to add your comment.
						</Alert>
					)
				}
				{
					this.state.success && (
						<Alert type='success'>
							Your comment was added.
						</Alert>
					)
				}
            </Form>
        </div>);
    }
}

Add.propTypes={
	contentId: 'string',
	contentType: 'string'
};
Add.icon='comment';
