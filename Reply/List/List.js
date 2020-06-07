import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Row from 'UI/Row';
import Column from 'UI/Column';
import Spacer from 'UI/Spacer';
import Time from 'UI/Time';
import Reactions from 'UI/Reactions';
import UserSignpost from 'UI/User/Signpost';

/**
 * A list of forum replies
 */

export default (props) =>
	<Loop over='forumreply/list' filter={props.threadId ? {where: {ThreadId: props.threadId}} : undefined} {...props}>
	{props.children.length ? props.children : reply => 
			<div className= "reply">
				<Column size = "12">
					<Row>
						<Column size = "9">
							{reply.creatorUser && (
								<UserSignpost user={reply.creatorUser} />
							)}
						</Column>
						<Spacer height = "30"/>
						<Column size = "3">
							Last Edited <Time date = {reply.editedUtc} />
						</Column>
					</Row>
					<Row>
						<Column size = "12">
							<div className="reply-body">
								<Canvas>
									{reply.bodyJson}
								</Canvas>
							</div>
						</Column>
					</Row>
					<Row>
						<Column>
							<Reactions on={reply} />
						</Column>
						
					</Row>
				</Column>
				
				<hr></hr>
			</div>
	}
	</Loop>