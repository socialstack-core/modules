import getRef from 'UI/Functions/GetRef';
import omit from 'UI/Functions/Omit';
import logoRef from './logo.png';

/*
Used to display an image from a fileRef.
Min required props: size, fileRef. Size must be an available size from your uploader config. You should pick the nearest bigger size from the physical size you're after.
<Image fileRef='public:2.jpg' size=100 />
*/
export default function Image (props) {
	const { onClick, fileRef, linkUrl, size, fullWidth, float } = props;

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
	
	var attribs = omit(props, ['fileRef', 'onClick', 'linkUrl', 'size', 'fullWidth']);
	attribs.alt = attribs.alt || attribs.title;
	var img = <div className={imageClass} onClick={props.onClick}>
		{getRef(props.fileRef, {attribs, size})}
	</div>;
	return linkUrl ? <a alt={attribs.alt} title={attribs.title} href={linkUrl}>{img}</a> : img;
}

Image.propTypes = {
	fileRef: {type: 'string', default: logoRef},
	linkUrl: 'string',
	title: 'string',
	fullWidth: 'bool',
	size: ['original', '2048', '1024', '512', '256', '200', '128', '100', '64', '32'], // todo: pull from api
	float: { type: ['None', 'Left', 'Right'], default: 'None' }
};

Image.icon = 'image';