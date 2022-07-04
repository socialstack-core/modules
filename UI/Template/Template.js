import Canvas from 'UI/Canvas';
import Content from 'UI/Content';

/**
 * Renders a template with a given ID, and substitutes one or more named subs.
 */
export default Template = (props) => {
	return <Content type="template" id={props.id}>
		{template => template ? <Canvas onSubstitute={(name) => {
				if(name == 'content' || !name){
					// The direct kids of the template.
					return props.children;
				}
				return props.tokens ? props.tokens[name] : null;
			}}>
				{
					template.bodyJson
				}
			</Canvas> : props.children
		}
	</Content>;
}

Template.propTypes = {
	id: {type: 'id', content: 'Template'},
	children: true
};