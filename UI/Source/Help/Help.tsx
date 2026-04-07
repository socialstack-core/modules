import Button from 'UI/Button';
import Popover from 'UI/Popover';
import Html from 'UI/Html';

/**
 * Props for the Help component.
 */
interface HelpProps {
	content?: HtmlString,
	id?: string
}

/**
 * The Help React component.
 * @param props React props.
 */
const Help: React.FC<HelpProps> = (props) => {
	const { content } = props;
	const id = props?.id || "page_help";

	if (!content) {
		return;
	}

	return <>
		<Button sm outlined variant="primary" className="ui-help__trigger" popoverTarget={id}>
			<i className="fr fr-question-circle"></i>
			<span>
				{`Need help?`}
			</span>
		</Button>
		<Popover method="auto" id={id} alignment="center" blurBackground={true} className="ui-help__wrapper">
			<div className="ui-help__content">
				<header className="ui-page__header">
					<h2 className="ui-page__subtitle">
						{`Help`}
					</h2>
					<Button xs variant="secondary" outlined popoverTarget={id} popoverTargetAction="hide">
						<i className="fr fr-times-alt"></i>
						<span className="sr-only">
							{`Close`}
						</span>
					</Button>
				</header>
				<Html>
					{content}
				</Html>
				<footer className="ui-page__footer">
					<Button popoverTarget={id} popoverTargetAction="hide">
						{`Close`}
					</Button>
				</footer>
			</div>
		</Popover>
	</>;
}

export default Help;