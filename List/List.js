import Loop from "UI/Loop";
import Add from "UI/Comments/Add";
import getContentTypeId from "UI/Functions/GetContentTypeId";
import Row from 'UI/Row';
import sinceDate from 'UI/Functions/SinceDate';
import Modal from 'UI/Modal';
import DeleteComment from 'UI/Comments/Delete';
import EditComment from 'UI/Comments/Edit';
import ReportComment from 'UI/Comments/Report';

export default class List extends React.Component{
	constructor(props){
		super(props);
		this.state={
			comments: [],
			deletedComments: [],
			editComments: []
		};
	}
	
	replyToggle(comment) {
		// Let's find out which comments are toggled.
		var comments = this.state.comments;
		comments[comment.id] = !comments[comment.id];

		this.setState({comments});
	}

	editToggle(comment) {
		var {editComments} = this.state; 
		editComments[comment.id] = !editComments[comment.id]; 
		
		this.setState({editComments});
	}



	onDeleted(comment) {
		var {deletedComments} = this.state;

		deletedComments[comment.id] = true;

		this.setState({deletedComments, deleteComment: null});
	}

    render(){

        let {
			contentId,
			contentType,
			on
		} = this.props;

		var {
			comments,
			deleteComment,
			reportComment,
			deletedComments,
			editComments
		} = this.state;
        
        if(!contentType && on){
			contentId = on.id;
			contentType = on.type;
		}
		
		if(!contentType){
            // Missing required props.
            console.log("contentType not set");
			return null;
        }
		
		var contentTypeId = getContentTypeId(contentType);
		
        return(
            <div className = "comments">
				<DeleteComment comment = {deleteComment} onSuccess = {() => this.onDeleted(deleteComment)} onClose = {() => this.setState({deleteComment: null})}/>
				<Modal classname = "comment-report-modal" title = "Report comment" visible = {reportComment} onClose = {() => {this.setState({reportComment: null})}}>
					<ReportComment onClose = {() => {this.setState({reportComment: null})}} comment = {reportComment}/>
				</Modal>
				<Add contentId={contentId} contentTypeId={contentTypeId} visible = {true} onSuccess = {() => {this.setState({hasComments: true})}}/>
				<Loop over='comment/list' filter={
					{
						where:{
							ContentId: on.id,
							ContentTypeId: contentTypeId,
							RootParentCommentId: 0
						},
						sort: {
							field: 'Order'
						}
					}	
				}
				groupAll
				orNone = {() =>{
					return <div>
						<h3>No Comments yet?</h3>
						<p>Be the first to leave your thoughts!</p>
					</div>
				}}
				onResults = {(results) => {
					var count = 0;
					results.forEach(result => {
						if(result.deleted == 0) {
							count++
						}
					});

					if(count > 0) {
						this.setState({hasComments: true});
					}

					return results;
				}}
				>
					{coms => {
						var { user } = this.context.app.state;
						
						var {hasComments} = this.state;

						if(!hasComments) {
							return <div>
								<h3>No Comments yet?</h3>
								<p>Be the first to leave your thoughts!</p>
							</div>
						}

						return coms.map(comment => {
							var isUser = user && comment.creatorUser && comment.creatorUser.id == user.id;
							var username = comment.creatorUser && comment.creatorUser.username && comment.creatorUser.username.length ? comment.creatorUser.username : "User";

							return <li style={{marginLeft: (comment.depth * 100) + 'px'}}>
								{deletedComments[comment.id] || comment.deleted ? (comment.childCommentCount - comment.childCommentDeleteCount > 0 && <i>-This comment was deleted-</i>) : <> 
									<Row className = "user-info"><b className = "user-name">{username}</b> {sinceDate(comment.createdUtc)}</Row>
									{editComments[comment.id] ? <EditComment 
										onSuccess = {() => this.editToggle(comment)}
										onClose = {() => this.editToggle(comment)}
										comment = {comment}
									/> : <> {comment.body}
										<Row className = "comment-actions">
											<button onClick = {() => {this.replyToggle(comment)}} className = "btn"><i class="far fa-comment-alt-lines"></i> Reply</button> 
											{isUser && <button onClick = {() => {this.editToggle(comment)}}className = "btn"><i class="fas fa-pencil"></i> Edit</button>}
											{!isUser && user && <button onCLick = {() => {this.setState({reportComment: comment})}} className = "btn"><i class="far fa-flag"></i> Report</button>}
											{isUser && <button onClick = {() => {this.setState({deleteComment: comment})}} className = "btn"><i class="far fa-trash-alt"></i> Delete</button>}
										</Row>
									</>}
								</>}
							<Add onClose = {() => {this.replyToggle(comment)}} visible = {comments[comment.id]} contentId={contentId} contentTypeId={contentTypeId} parentCommentId={comment.id} />
						</li>;
						
					});
				}}
				</Loop>
        </div>);
    }
}

List.propTypes={
	contentId: 'string',
	contentType: 'string'
};
List.icon='comment';
