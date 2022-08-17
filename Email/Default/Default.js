import {SessionConsumer} from 'UI/Session';

export default class Default extends React.Component {
	
	render(){
		return <SessionConsumer>
			{session => this.renderIntl(session)}
		</SessionConsumer>
	}

	renderIntl(session){
		var user = session.user ? session.user : {};
		
		if(!user){
			user={};
		}
		var { origin } = global.location;

		
		return <table border="0" cellpadding="0" cellspacing="0" id="body" style={{textAlign: "center", minWidth: "640px", width: "100%", margin: "0", padding: "0"}} bgcolor="#f0f3f7">
			<tbody>
				<tr class="line">
					<td style={{fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif", height: "4px", fontSize:"4px", lineHeight: "4px"}} bgcolor="#7068d6"></td>
				</tr>
				<tr class="header">
					<td style={{fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif", fontSize: "13px", lineHeight: "1.6", color: "#5c5c5c", padding: "25px 0"}}>
						{!this.props.hideLogo && (this.props.customLogo || <img alt="" src={origin + "/email_logo.png"} width="55" height="50" />)}
					</td>
				</tr>
				<tr>
					<td style={{fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif"}}>
						<table border="0" cellpadding="0" cellspacing="0" class="wrapper" style={{width: "640px", borderCollapse: "separate", borderSpacing: "0", margin: "0 auto"}}>
							<tbody style = {{padding: "1rem"}}>
								<tr>
									<td class="wrapper-cell"  align="left" bgcolor="#ffffff" style={{ borderRadius: "3px", overflow: "hidden", padding: "1rem", border: "1px solid #ededed"}}>
										{!this.props.hideGreeting && (
											<p>
												Hi {user.firstName},
											</p>
										)}
										{this.props.children}
									</td>
								</tr>
							</tbody>
						</table>
					</td>
				</tr>
				{!this.props.hideFooter && <tr class="footer">
					<td style={{fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif", fontSize: "13px", lineHeight: "1.6", color: "#5c5c5c", padding: "25px 0"}}>
						<div>
							You're receiving this email because of your account with us. <a class="mng-notif-link" href={origin + "/email/preferences"} style={{color: "#3777b0", textDecoration: "none"}}>Change preferences here</a>
						</div>
					</td>
				</tr>}
				<tr>
					<td class="footer-message" style={{fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif", fontSize: "13px", lineHeight: "1.6", color: "#5c5c5c", padding: "25px 0"}}>
					</td>
				</tr>
			</tbody>
		</table>;
	}
	
}

Default.propTypes = {
	children: true,
	hideGreeting: 'bool',
	customLogo: 'jsx',
	hideLogo: 'bool',
	hideFooter: 'bool'
};

Default.groups = 'email';