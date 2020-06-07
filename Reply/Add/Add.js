import Input from "UI/Input";
import Form from "UI/Form";


export default class Add extends React.Component{
	
	constructor(props){
		super(props);
		this.state={};
	}
	
    render(){

        let {
			threadId,
		} = this.props;
		
		if(!threadId){
            // Missing required props.
            console.log("threadId not set");
			return null;
        }
		
        return(
            <div>
            <Form 
                action = "forumreply" 
                onFailure = {response => {
					this.setState({failed: true});
				}}
                onValues = { values => {return {
                    ...values,
                    threadId: threadId,
                    bodyJson: JSON.stringify({content: values.bodyJson})
                }}}
            >
                <Input type="canvas" name = "bodyJson"/>
                <Input type="submit" label="Reply"/>
            </Form>
        </div>);
    }
}