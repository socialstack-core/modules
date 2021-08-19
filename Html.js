/**
 * This component displays html. Only ever use this with trusted text.
*/

export default function Html (props) {
   return <span dangerouslySetInnerHTML={{__html: props.html || props.children}} />;
}

Html.propTypes={
	html: 'text'
};
