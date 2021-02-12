import Input from "UI/Input";
import Form from "UI/Form";
import Alert from "UI/Alert";

export default class Add extends React.Component{
	constructor(props){
		super(props);
		this.state={};
	}
	
    render(){

        let {
			contentId,
			contentTypeId,
			parentCommentId // (optional)
		} = this.props;
        
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
                <Input type="textarea" name = "bodyJson"/>
                <Input type="submit" label="Comment"/>
				
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
