import omit from 'UI/Functions/Omit';

/**
 * For h1/h2/h3 etc.
 */
export default function Heading () {
	var Mod = 'h' + (props.size || '1');
	var className = 'heading ' + (props.className || '');
	return <Mod {...omit(props, ['children', 'className'])}>{props.children}</Mod>;
}

Heading.propTypes={
	size: ['1','2','3','4','5','6'],
	children: {default: 'My New Heading'}
}

Heading.icon='heading';