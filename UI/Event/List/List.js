import Loop from 'UI/Loop';
import Column from 'UI/Column';
import Row from 'UI/Row';
import url from 'UI/Functions/Url';

/**
 * A list of events
 */

export default (props) =>
	<Loop over='event/list' {...props}>
	{props.children.length ? props.children : event => 
		<a href={url(event)}>
			<Row>
				<Column size='12'>
					<h3>
					{event.name}
					</h3>
				</Column>
			</Row>
		</a>
	}
	</Loop>