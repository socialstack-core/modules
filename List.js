import Loop from 'UI/Loop';
import Column from 'UI/Column';
import Row from 'UI/Row';
import SinceDate from 'UI/SinceDate';
import UpDownVote from 'UI/UpDownVote';
import UserSignpost from 'UI/User/Signpost';
import StoryAttachmentList from 'UI/Story/Attachment/List';
import Canvas from 'UI/Canvas';

/**
 * A list of feed stories
 */

export default (props) =>
	<div className = 'feed-story-list'>
		<hr/>
		<h3>My Activity</h3>
		<Loop over='feed/story/list' filter={props.userId ? {where: {UserId: props.userId}} : undefined} {...props} orNone={() => "This user likes to be stealthy."}>
		{props.children.length ? props.children : story => {
			
			return <div className={"story"}>
				<div className="created-date">
					<SinceDate date={story.createdUtc}/>
				</div>
				<Canvas>
					{story.bodyJson}
				</Canvas>
				<StoryAttachmentList on={story} />
			</div>
			}
		}
		</Loop>
		
	</div>