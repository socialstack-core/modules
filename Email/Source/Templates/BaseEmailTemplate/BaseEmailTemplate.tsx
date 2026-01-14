import { useSession } from 'UI/Session';

/**
 * Props for the default template.
 */
interface DefaultProps {
	/**
	 * True to hide the footer.
	 */
	hideFooter?: boolean,
	/**
	 * True to hide the logo.
	 */
	hideLogo?: boolean,

	/**
	 * True to hide the greeting.
	 */
	hideGreeting?: boolean,

	/**
	 * Optionally replace the logo with something else. 
	 * If hideLogo is true this will not appear either.
	 */
	customLogo?: React.ReactNode
}

/**
* The main default email template.
*/
const Default: React.FC<React.PropsWithChildren<DefaultProps>> = (props) => {
	const {
		hideFooter,
		hideLogo,
		children,
		hideGreeting,
		customLogo
	} = props;
	const { session } = useSession();
	var user = session.user;
	var { origin } = window.location;

	return <table border={0} cellPadding={0} cellSpacing={0} id="body" style={{
		textAlign: "center",
		minWidth: "640px",
		width: "100%",
		margin: "0",
		padding: "0"
	}} bgcolor="#f0f3f7">
		<tbody>
			<tr className="line">
				<td style={{
					fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif",
					height: "4px",
					fontSize: "4px",
					lineHeight: "4px",
					background: "#7068d6"
				}}></td>
			</tr>
			<tr className="header">
				<td style={{
					fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif",
					fontSize: "13px",
					lineHeight: "1.6",
					color: "#5c5c5c",
					padding: "25px 0"
				}}>
					{!hideLogo && (customLogo || <img alt="" src={origin + "/email_logo.png"} width="55" height="50" />)}
				</td>
			</tr>
			<tr>
				<td style={{fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif"}}>
					<table border={0} cellPadding={0} cellSpacing={0} className="wrapper" style={{
						width: "640px",
						borderCollapse: "separate",
						borderSpacing: "0",
						margin: "0 auto"
					}}>
						<tbody style = {{padding: "1rem"}}>
							<tr>
								<td className="wrapper-cell" align="left" style={{
									borderRadius: "3px",
									overflow: "hidden",
									padding: "1rem",
									border: "1px solid #ededed",
									background: "#ffffff"
								}}>
									{!hideGreeting && user && (
										<p>
											Hi {user?.username},
										</p>
									)}
									{children}
								</td>
							</tr>
						</tbody>
					</table>
				</td>
			</tr>
			{!hideFooter && <tr className="footer">
				<td style={{
					fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif",
					fontSize: "13px",
					lineHeight: "1.6",
					color: "#5c5c5c",
					padding: "25px 0"
				}}>
					<div>
						You're receiving this email because of your account with us. <a className="mng-notif-link" href={origin + "/email/preferences"} style={{
							color: "#3777b0",
							textDecoration: "none"
						}}>Change preferences here</a>
					</div>
				</td>
			</tr>}
			<tr>
				<td className="footer-message" style={{
					fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif",
					fontSize: "13px",
					lineHeight: "1.6",
					color: "#5c5c5c",
					padding: "25px 0"
				}}>
				</td>
			</tr>
		</tbody>
	</table>;
}

export default Default;