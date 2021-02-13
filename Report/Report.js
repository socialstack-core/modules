export default class Report extends React.Component{
	constructor(props){
		super(props);
		this.state={};
	}

    render(){
        return(
            <div className = "comment-report">
				<Form 
					action = "report" 
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
