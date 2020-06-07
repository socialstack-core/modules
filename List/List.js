import Loop from 'UI/Loop';
import Column from 'UI/Column';
import Time from 'UI/Time';
import Row from 'UI/Row';
import url from 'UI/Functions/Url';

/**
 * A list of forums
 */

export default (props) =>
	<div class = "forum-list">
		<Row>
			<Column size="6"><h5>Topic</h5></Column>
			<Column size="2"><h5>Thread Count</h5></Column>	
			<Column size="2"><h5>Reply Count</h5></Column>	
			<Column size="2"><h5>Latest Reply</h5></Column>	
		</Row>
		<hr></hr>	
		<Loop over='forum/list' {...props}>
		{props.children.length ? props.children : forum => 
			<a href={url(forum)}>
				<Row>
					<Column size='6'>
						<h3>
							{forum.name}
						</h3>
						<p>
							{forum.description}
						</p>
					</Column>
					<Column size='2'>
						{forum.threadCount}
					</Column>
					<Column size='2'>
						{forum.replyCount}
					</Column>
					<Column size='2'>
						<Time date={forum.latestReplyUtc} />
					</Column>
				</Row>
				<hr></hr>
			</a>
			
		}
		
		</Loop>
		
	</div>