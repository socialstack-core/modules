import Loop from 'UI/Loop';
import Column from 'UI/Column';
import Time from 'UI/Time';
import Row from 'UI/Row';
import url from 'UI/Functions/Url';

/**
 * A list of forums
 */

export default class List extends React.Component {

	// If you want to use state in your react component, uncomment this constructor:
	constructor(props){
		super(props);
		this.state = {};
	}

	render() {
		
		var {showHeader, children} = this.props;

		return (
			<div class = "forum-list">
				{showHeader ? 
					<Row className="forum-list-header">
						<Column size="6"><h5>Topic</h5></Column>
						<Column size="2"><h5>Thread Count</h5></Column>	
						<Column size="2"><h5>Reply Count</h5></Column>	
						<Column size="2"><h5>Latest Reply</h5></Column>	
					</Row> : undefined
				}
				
				<hr></hr>	
				<Loop over='forum/list' {...this.props}>
				{children && children.length ? children : forum => 
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
		);
	}
}

List.propTypes = {
	showHeader: 'boolean'
};
List.defaultProps = {
	showHeader: true
};
List.icon='align-center';

/*
export default (props) =>
	<div class = "forum-list">
		<Row className="forum-list-header">
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
*/