import Input from "UI/Input";
import Form from "UI/Form";


export default class Add extends React.Component {
	
	constructor(props){
		super(props);
		this.state={};
	}
	
    render(){

        var {threadId} = this.props;
		
		if(!threadId){
            // Missing required props.
            console.log("threadId not set");
			return null;
        }
		
        return(
            <div className="reply-add">
                <Form 
                    action = "forumreply" 
                    onFailure = {response => {
    					this.setState({failed: true});
    				}}
    				onSuccess = {
    					() => {
    						document.getElementById('_replyArea').value='';
    					}
    				}
                    onValues = { values => {return {
                        ...values,
                        threadId: threadId,
                        bodyJson: JSON.stringify({content: values.body})
                    }}}
                >
                    <Input id="_replyArea" type="textarea" name = "body"/>
                    <Input type="submit" label="Reply" className="btn btn-dark"/>
                </Form>
            </div>
        );
    }
}

Add.propTypes = {
    threadId: 'int'
};
Add.defaultProps = {};