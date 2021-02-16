export default function Failed () {
	return(
		<div className="alert alert-danger" role="alert" style = {{textAlign: "center"}}>
			<i className="fad fa-wifi-slash" />
			<p>The service is currently unavailable. This may be because your device is currently offline.</p>
		</div>
	);
}