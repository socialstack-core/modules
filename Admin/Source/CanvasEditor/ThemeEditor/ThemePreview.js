import Button from 'UI/Button';

export default function ThemePreview(props) {
	
	var btn = null;
	var {theme} = props;
	var {data} = theme;
	var key = data.key || data.Key;
	
	switch(props.previewButton){
		default:
		case 1:
			btn = <Button variant="primary">{`Primary`}</Button>;
		break;
		case 2:
			btn = <Button variant="secondary">{`Secondary`}</Button>;
		break;
		case 3:
			btn = <Button variant="success">{`Success`}</Button>;
		break;
		case 4:
			btn = <Button variant="danger">{`Danger`}</Button>;
		break;
		case 5:
			btn = <Button variant="warning">{`Warning`}</Button>;
		break;
		case 6:
			btn = <Button variant="info">{`Info`}</Button>;
		break;
		case 7:
			btn = <Button variant="light">{`Light`}</Button>;
		break;
		case 8:
			btn = <Button variant="dark">{`Dark`}</Button>;
		break;
	}
	
	var className = "theme-editor__theme-preview";
	
	if(props.selected){
		className += " theme-editor__theme-preview__selected";
	}
	
	return <button className={className} data-theme={key} onClick={props.onClick}>
		<p>
			Hello world.
		</p>
		{btn}
	</button>;
	
}