import Input from "UI/Input";
import Form from "UI/Form";
import url from 'UI/Functions/Url';


export default class Add extends React.Component{
	
	constructor(props) {
		super(props);
		this.state={};
	}
	
    render() {

        var {forumId} = this.props;
		
		if (!forumId) {
            // Missing required props.
            console.log("forumId not set");
			return null;
        }
		
        return (
            <div className="add">
                <Form 
                    action="forumthread" 
                    onFailure={response => {
    					this.setState({failed: true});
    				}}
    				onSuccess={response => {
    					// Go to the new thread:
    					global.pageRouter.go(url(response));
    				}}
                    onValues={values => {
                        return {
                            ...values,
                            forumId: forumId,
                            bodyJson: JSON.stringify({content: values.body})
                        }
                    }}
                >
    				<Input type="text" name = "title" label="Thread Title" placeholder="My awesome thread"/>
                    <Input type="textarea" name = "body"/>
                    <Input type="submit" label="Create thread" className="btn btn-dark"/>
                </Form>
            </div>
        );
    }
}

Add.propTypes = {
    forumId: 'int'
};
Add.defaultProps = {};