export default function ThemePreview(props){
	
	var btn = null;
	var {theme} = props;
	var {data} = theme;
	var key = data.key || data.Key;
	
	switch(props.previewButton){
		default:
		case 1:
			btn = <button className="btn btn-primary">{`Primary`}</button>;
		break;
		case 2:
			btn = <button className="btn btn-secondary">{`Secondary`}</button>;
		break;
		case 3:
			btn = <button className="btn btn-success">{`Success`}</button>;
		break;
		case 4:
			btn = <button className="btn btn-danger">{`Danger`}</button>;
		break;
		case 5:
			btn = <button className="btn btn-warning">{`Warning`}</button>;
		break;
		case 6:
			btn = <button className="btn btn-info">{`Info`}</button>;
		break;
		case 7:
			btn = <button className="btn btn-light">{`Light`}</button>;
		break;
		case 8:
			btn = <button className="btn btn-dark">{`Dark`}</button>;
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