import getRef from 'UI/Functions/GetRef';
import omit from 'UI/Functions/Omit';

/*
Used to display an image from a fileRef.
Min required props: size, fileRef. Size must be an available size from your uploader config. You should pick the nearest bigger size from the physical size you're after.
<Image fileRef='public:2.jpg' size=100 />
*/
export default class Image extends React.Component {
	render(){
		return <div className="image" onClick={this.props.onClick}>{getRef(this.props.fileRef, {...(omit(this.props, ['fileRef', 'onClick']))})}</div>;
	}
}

Image.propTypes = {
	fileRef: 'string',
	size: 'int'
};