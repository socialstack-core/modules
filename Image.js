import getRef from 'UI/Functions/GetRef';
import omit from 'UI/Functions/Omit';

/*
Used to display an image from a fileRef.
Min required props: size, fileRef. Size must be an available size from your uploader config. You should pick the nearest bigger size from the physical size you're after.
<Image fileRef='public:2.jpg' size=100 />
*/
export default class Image extends React.Component {
	render(){
		const {onClick, fileRef, linkUrl, size} = this.props;
		
		var attribs = omit(this.props, ['fileRef', 'onClick', 'linkUrl', 'size']);
		attribs.alt = attribs.alt || attribs.title;
		var img = <div className="image" onClick={this.props.onClick}>{getRef(this.props.fileRef, {attribs, size})}</div>;
		return linkUrl ? <a alt={attribs.alt} title={attribs.title} href={linkUrl}>{img}</a> : img;
	}
}

Image.propTypes = {
	fileRef: 'string',
	linkUrl: 'string',
	title: 'string',
	size: ['original', '1024', '512', '256', '200', '128', '100', '64', '32'] // todo: pull from api
};

Image.icon = 'image';