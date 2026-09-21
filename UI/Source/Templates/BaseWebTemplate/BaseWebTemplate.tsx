interface BaseWebTemplateProps {
	header?: React.ReactNode,
	body?: React.ReactNode,
	footer?: React.ReactNode,
}

const BaseWebTemplate: React.FC<BaseWebTemplateProps> = (props: BaseWebTemplateProps) => {

	return (
		<div id="site-wrapper">
			{/* site header / nav */}
			{props.header && <>
				<nav role="navigation" aria-label={`Main navigation`}>
					{props.header}
				</nav>
			</>}

			{/* main site content */}
			<main role="main" id="site-content">
				{props.body}
			</main>

			{/* site footer */}
			{props.footer && <>
				<footer role="contentinfo">
					{props.footer}
				</footer>
			</>}
		</div>
	)
};

export default BaseWebTemplate;