import Row from 'UI/Row';
import Modal from 'UI/Modal';
import Form from 'UI/Form';
import Input from 'UI/Input';

export default class Edit extends React.Component{
	constructor(props){
		super(props);
		this.state={};
	}

	close() {
		this.props.onClose && this.props.onClose();
	}
	
    render(){
        var {comment} = this.props;

        var value = JSON.parse(comment.bodyJson);
        console.log(comment);
        console.log(value);

        return(
            <div className = "comment-edit">
                 <Form
                    action = {"comment/"+comment.id}
                    onFailure= {
                        response => {
                            this.setState({failure: true, submitting: false});
                        }
                    }
                    onSuccess = {
                        (response, values, e) => {
                            this.setState({success: true, failure: false, submitting: false});
                            this.props.onSuccess && this.props.onSuccess();
                        }
                    }
                    onValues = {
                        values => {
                            values.bodyJson = JSON.stringify({ content: values.bodyJson });

                            this.setState({success: false, failure: false, submitting: true})
                            
                            return {
                                ...values
                            }
                        }
                    }
                >
                    <Input placeholder="Leave us your thoughts!" name = "bodyJson" type = "textarea" defaultValue = {value.content} validate={["Required"]}/>
                    <Row className = "comment-buttons">
                        <Input type="submit" label="Save changes"/>
                        <div className = "cancel-button form-group">
                            <button className = "btn btn-danger" onClick = {() => {this.close();}}>Cancel</button>
                        </div>
                    </Row>
                </Form>
            </div>
        );
    }
}
