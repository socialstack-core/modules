export default class Default extends React.Component {
	
	render(){
		var user = global.app.state.user || {};
		var { origin } = global.location;
		
		return <table border="0" cellpadding="0" cellspacing="0" id="body" style="text-align: center; min-width: 640px; width: 100%; margin: 0; padding: 0;" bgcolor="#f0f3f7">
			<tbody>
				<tr class="line">
					<td style="font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; height: 4px; font-size: 4px; line-height: 4px;" bgcolor="#7068d6"></td>
				</tr>
				<tr class="header">
					<td style="font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; font-size: 13px; line-height: 1.6; color: #5c5c5c; padding: 25px 0;">
						<img alt="" src={origin + "/email_logo.png"} width="55" height="50" />
					</td>
				</tr>
				<tr>
					<td style="font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif;">
						<table border="0" cellpadding="0" cellspacing="0" class="wrapper" style="width: 640px; border-collapse: separate; border-spacing: 0; margin: 0 auto;">
							<tbody>
								<tr>
									<td class="wrapper-cell" style="font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; border-radius: 3px; overflow: hidden; padding: 18px 25px; border: 1px solid #ededed;" align="left" bgcolor="#ffffff">
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
				<tr class="footer">
					<td style="font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; font-size: 13px; line-height: 1.6; color: #5c5c5c; padding: 25px 0;">
						<div>
							You're receiving this email because of your account with us. <a class="mng-notif-link" href={origin + "/email/preferences"} style="color: #3777b0; text-decoration: none;">Change preferences here</a>
						</div>
					</td>
				</tr>
				<tr>
					<td class="footer-message" style="font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; font-size: 13px; line-height: 1.6; color: #5c5c5c; padding: 25px 0;">
					</td>
				</tr>
			</tbody>
		</table>;
	}
	
}

Default.propTypes = {
	children: true,
	hideGreeting: 'bool'
};