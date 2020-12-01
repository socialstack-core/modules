import omit from 'UI/Functions/Omit';

export default class Paragraph extends React.Component {
    constructor(props) {
        super(props);
    }
	
    render() {
		return <p data-aos="fade-up" {...(omit(this.props, ['children']))}>{this.props.children}</p>;
    }
}

Paragraph.propTypes = {
	children: true
};
Paragraph.icon = 'align-left';
