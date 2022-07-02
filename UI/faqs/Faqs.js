import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Collapsible from 'UI/Collapsible';
import Container from 'UI/Container';

const DEFAULT_HEADING_LEVEL = 2;

export default class Faqs extends React.Component {

	renderInternal(HeadingNode, title) {
		return <>
			<HeadingNode className="faq-list-title">
				{title}
			</HeadingNode>
			<Loop over='frequentlyaskedquestion/list'>
				{faq => {

					return <Collapsible title={faq.question}>
						<Canvas>
							{faq.answerJson}
						</Canvas>
					</Collapsible>;

				}}
			</Loop>
		</>;
    }

	render() {
		var { title, headingLevel, hasContainer, id } = this.props;
		
		headingLevel = !headingLevel ? DEFAULT_HEADING_LEVEL : parseInt(headingLevel, 10);
		
		if (isNaN(headingLevel)) {
			headingLevel = DEFAULT_HEADING_LEVEL;			
		}
		
		var HeadingNode = 'h' + headingLevel;
		
		return <section className="faq-list" id={id}>
			{hasContainer && <>
				<Container>
					{this.renderInternal(HeadingNode, title)}
				</Container>
			</>}

			{!hasContainer && <>
				{this.renderInternal(HeadingNode, title)}
			</>}
		</section>;
		
	}
	
}

Faqs.propTypes = {
	title: 'string',
	headingLevel: [1, 2, 3, 4, 5, 6],
	hasContainer: 'bool',
	id: 'string'
};

Faqs.defaultProps = {
	title: `Frequently Asked Questions`,
	headingLevel: DEFAULT_HEADING_LEVEL,
	hasContainer: true,
	id: 'faq'
};