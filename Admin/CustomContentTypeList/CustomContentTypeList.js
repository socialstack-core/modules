import Loop from 'UI/Loop';
import Default from 'Admin/Layouts/Default';
import Tile from 'Admin/Tile';
import Col from 'UI/Column';
import Row from 'UI/Row';

export default class CustomContentTypeList extends React.Component {
	
	render() {
		return (
			<Default>
				<Tile className="custom-content-type-list" title="Manage Your Data">
					<Loop 
							over='customContentType' 
							live
						>
							{
								contentType => {
										return <div className="custom-content-type--entry">
											<a href={"/en-admin/" + contentType.name.replace(/\s/g, "")} className="custom-content-type--entry-link">{contentType.nickName}</a>
										</div>
								}
							}
					</Loop>
				</Tile>
			</Default>
		);
	}
	
}

/*
// propTypes are used to describe configuration on your component in the editor.
// Just setting it to an empty object will make your component appear as something that can be added.
// Define your available props like the examples below.

CustomContentTypeList.propTypes = {
	
	title: 'string', // text input
	size: [1,2,3,4], // dropdowns
	
	// All <Input type='x' /> values are supported - checkbox, color etc.
	// Also the special id type which can be used to select some other piece of content (by customContentTypeList name), like this:
	templateToUse: {type: 'id', content: 'Template'}
	
};

CustomContentTypeList.icon='align-center'; // fontawesome icon
*/