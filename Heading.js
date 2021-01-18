import omit from 'UI/Functions/Omit';
import userAgent from 'UI/Functions/UserAgent';

/**
 * For h1/h2/h3 etc.
 */
export default class Heading extends React.Component {
	
	render() {
		var Mod = 'h' + (this.props.size || '1');
		var className = 'heading ' + (this.props.className || '');

		// fade vertically on mobile to prevent triggering horizontal scrolling issues
		var fadeDirection = userAgent.isMobile() ? "fade-up" : "fade-right";
		
		return <Mod data-aos={fadeDirection} {...omit(this.props, ['children', 'className'])}>{this.props.children}</Mod>;
	}
	
}

Heading.propTypes={
	size: ['1','2','3','4','5','6'],
	children: {default: 'My New Heading'}
}

Heading.icon='heading';