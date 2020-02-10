import getRef from 'UI/Functions/GetRef';
import omit from 'UI/Functions/Omit';

/*
Used to display an image from a fileRef.
Min required props: size, fileRef. Size must be an available size from your uploader config. You should pick the nearest bigger size from the physical size you're after.
<Image fileRef='public:2.jpg' size=100 />
*/
export default (props) => <div className="image" onClick={props.onClick}>{getRef(props.fileRef, {...(omit(props, ['fileRef', 'onClick']))})}</div>