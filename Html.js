/**
 * This component displays html. Only ever use this with trusted text.
*/

export default function Html (props) {
   return <span {...props} dangerouslySetInnerHTML={{__html: props.html || props.children}} />;
}

Html.propTypes={
	html: 'text'
};
