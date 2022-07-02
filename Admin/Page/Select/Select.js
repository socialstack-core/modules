import getPages from 'Admin/Functions/GetPages';
import Input from 'UI/Input';

/**
 * Dropdown to select a page by URL. Value is the page ID.
 */
export default class Select extends React.Component {
	
	render(){
		
		var pages = getPages();
		
		return (<Input {...this.props} type="select">{
			pages.map(pg => <option value={pg.id}>{pg.url}</option>)
		}</Input>);
		
	}
	
}