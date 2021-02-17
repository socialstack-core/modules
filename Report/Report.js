import Form from 'UI/Form';
import Input from 'UI/Input';
import Row from 'UI/Row';

export default class Report extends React.Component{
	constructor(props){
		super(props);
		this.state={};
	}

	close() {
		this.props.onClose && this.props.onClose();
	}

    render(){
        return(
            <div className = "comment-report">
				<Form 
					action = "userflag" 
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
						contentTypeId
					}}}
				>
                    <Input placeholder="Leave us your thoughts!" type="textarea" name = "bodyJson"/>
					<Row className = "comment-buttons">
						<Input type="submit" label="Report"/>
						<div className = "cancel-button form-group">
							<button className = "btn btn-danger" onClick = {() => {this.close();}}>Cancel</button>
						</div>
					</Row>
                </Form>
            </div>
        );
    }
}

Report.propTypes={
	contentId: 'string',
	contentType: 'string'
};
Report.icon='comment';
