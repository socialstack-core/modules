import omit from 'UI/Functions/Omit';

export default function Paragraph (props) {
	return <p data-aos="fade-up" {...(omit(props, ['children']))}>{props.children}</p>;
}

Paragraph.propTypes = {
	children: true
};
Paragraph.icon = 'align-left';
