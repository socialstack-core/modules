import QrCode from 'UI/QrCode';
import Input from 'UI/Input';


export default function QrPreview (props) {
	var origin = location.origin;
	var id = props.currentContent ? props.currentContent.id : '';
	var url = origin + '/qr/' + id;
	
	return <div className="qr-preview">
		<Input {...props} />
		{id && <QrCode text={url} width={512} height={512}/>}
		{id && <p>{url}</p>}
	</div>;
}