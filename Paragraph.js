import omit from 'UI/Functions/Omit';

export default function Paragraph (props) {
	const { bold, animate } = props;

	var className = props.className || undefined;
	var animation = animate ? "fade-up" : undefined;

	return <p data-aos={animation} className={className} {...(omit(props, ['children']))}>
			{bold ? <strong>{props.children}</strong> : <>{props.children}</>}
		</p>;
}

Paragraph.propTypes = {
	children: true,
	bold: 'boolean',
	animate: 'boolean'
};

Paragraph.defaultProps = {
	bold: false,
	animate: true
};

Paragraph.icon = 'align-left';
