import Form from 'UI/Form';
import Input from 'UI/Input';
import Row from 'UI/Row';
import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import getContentTypeId from 'UI/Functions/GetContentTypeId';
import webRequest from 'UI/Functions/WebRequest';

export default class Report extends React.Component{
	constructor(props){
		super(props);
		this.state={};
	
		this.load(props);
	}

	load(props) {
		webRequest("userflag/list", {where: {ContentId: props.comment.id, ContentTypeId: getContentTypeId(props.comment.type), UserId: global.app.state.user.id}}).then(response => {
			// Are there already any userflags from this user on this content?
			if(response.json.total > 0 ) {
				this.setState({existingReport: response.json.results[0]});
			}
			
		}).catch(error => {
			console.log(error);
		});
	}

	undo() {
		var opts = {method: "delete"};
		webRequest("userflag/"+ this.state.existingReport.id, undefined, opts).then(response => {
			this.props.onClose && this.props.onClose();

		}).catch(error => {
			console.log(error);
		});
	}

	close() {
		this.props.onClose && this.props.onClose();
	}

    render(){
		var {existingReport} = this.state;

        return(
            <div className = "comment-report">
				{existingReport ? <div>
					<p>
						Thank you for reporting the incident. Someone will investigate!
					</p>
					<Row className = "comment-buttons">
						<button className = "btn btn-primary" onClick = {() => {this.undo()}}>
							Revoke report
						</button>
						
						<button className = "cancel-button btn btn-danger" onClick = {() => {this.close()}}>Close</button>

					</Row>
				</div> : <div>
					<p>
						Something amiss? Please let us know what is happening and we'll look into it.
					</p>
					<Form 
						action = "userflag" 
						onFailure={
							response => {
								this.setState({failure: true, submitting: false});
							}
						}	
						onSuccess={
							(response, values, e) => {
								this.setState({success: true, failure: false, submitting: false, existingReport: response});
								e.target.reset();
							}
						}
						onValues = { values => {
							return {
								contentId: this.props.comment.id,
								contentTypeId: getContentTypeId(this.props.comment.type),
								userFlagOptionId: this.state.option.id
							}
						}}
					>
						<Loop
							over = "userflagoption/list"
						>
							{
								option => {
									return <Input 
										id = {option.id}
										type = "radio"
										name = {"option_" + option.id}
										onChange={e => {
											this.setState({
												option
											});
										}}
										value={this.state.option == option}
										defaultValue={this.state.option == option}
										label = {
											<Canvas>
												{option.bodyJson}
											</Canvas>
										}
									/>
								}
							}
						</Loop>

						<Row className = "comment-buttons">
							<Input disabled = {!this.state.option} type="submit" label="Report"/>
							<div className = "cancel-button form-group">
								<button className = "btn btn-danger" onClick = {() => {this.close()}}>Cancel</button>
							</div>
						</Row>
					</Form>
				</div>}
            </div>
        );
    }
}

Report.propTypes={
	contentId: 'string',
	contentType: 'string'
};
Report.icon='comment';
