export default class Failed extends React.Component{
    render(){
        return(
            <div class="alert alert-danger" role="alert" style = {{textAlign: "center"}}>
                <i class="fad fa-wifi-slash"></i>
                <p>The service is currently unavailable. This may be because your device is currently offline.</p>
            </div>
        );
    }
}