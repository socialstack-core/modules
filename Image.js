import getRef from 'UI/Functions/GetRef';
import omit from 'UI/Functions/Omit';
import logo from './logo.png';

/*
Used to display an image from a fileRef.
Min required props: size, fileRef. Size must be an available size from your uploader config. You should pick the nearest bigger size from the physical size you're after.
<Image fileRef='public:2.jpg' size=100 />
*/
export default class Image extends React.Component {
	render(){
		const { onClick, fileRef, linkUrl, size, fullWidth, float } = this.props;

		var imageClass = "image";

		switch (float) {
			case "Left":
				imageClass += " image-left";
				break;

			case "Right":
				imageClass += " image-right";
				break;
		}

		if (fullWidth) {
			imageClass += " image-wide";
		}
		
		var attribs = omit(this.props, ['fileRef', 'onClick', 'linkUrl', 'size', 'fullWidth']);
		attribs.alt = attribs.alt || attribs.title;
		var img = <div className={imageClass} onClick={this.props.onClick}>
			{getRef(this.props.fileRef, {attribs, size})}
		</div>;
		return linkUrl ? <a alt={attribs.alt} title={attribs.title} href={linkUrl}>{img}</a> : img;
	}
}

Image.propTypes = {
	fileRef: {type: 'string', default: 'url:' + logo},
	linkUrl: 'string',
	title: 'string',
	fullWidth: 'bool',
	size: ['original', '2048', '1024', '512', '256', '200', '128', '100', '64', '32'], // todo: pull from api
	float: { type: ['None', 'Left', 'Right'], default: 'None' }
};

Image.icon = 'image';