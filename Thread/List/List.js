import Loop from 'UI/Loop';
import Column from 'UI/Column';
import Row from 'UI/Row';
import url from 'UI/Functions/Url';
import Time from 'UI/Time';
import Tags from 'UI/Tags';
import Spacer from 'UI/Spacer';

/**
 * A list of forum threads
 */

export default (props) =>
	<div>
		<a className="btn btn-primary" href={'/forum/' + props.forumId + '/create'}>Create a thread</a>
		<Spacer height='20'/>
		<div className = 'thread-list'>
			<Row>
				<Column size="6"><h5>Topic</h5></Column>
				<Column size="3"><h5>Author</h5></Column>	
				<Column size="3"><h5>Last Edited</h5></Column>	
			</Row>
			<hr></hr>
			<Loop over='forumthread/list' filter={props.forumId ? {where: {ForumId: props.forumId}} : undefined} {...props}>
			{props.children.length ? props.children : thread => 
				<a href={url(thread)}>
					<div>
					<Row>
						<Column size="6">
							<h2>{thread.title}</h2>
							<Tags on={thread} />
						</Column>
						<Column size="3">
							{thread.creatorUser ? thread.creatorUser.username : ""}
						</Column>
						<Column size="3">
							<Time date = {thread.lastReplyUtc} />
						</Column>
					</Row>
					</div>
					<hr></hr>
				</a>
				
			}
			</Loop>
		</div>
	</div>