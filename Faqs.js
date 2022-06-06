import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Collapsible from 'UI/Collapsible';

const DEFAULT_HEADING_LEVEL = 2;

export default class Faqs extends React.Component {

	render(){
		var { title, headingLevel } = this.props;
		
		headingLevel = !headingLevel ? DEFAULT_HEADING_LEVEL : parseInt(headingLevel, 10);
		
		if (isNaN(headingLevel)) {
			headingLevel = DEFAULT_HEADING_LEVEL;			
		}
		
		var HeadingNode = 'h' + headingLevel;
		
		return <div className="faq-list">
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
		</div>;
		
	}
	
}

Faqs.propTypes = {
	title: "string",
	headingLevel: [1, 2, 3, 4, 5, 6]
};

Faqs.defaultProps = {
	title: `Frequently Asked Questions`,
	headingLevel: DEFAULT_HEADING_LEVEL
};