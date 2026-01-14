import { useTokens } from 'UI/Token';

/**
 * Props for PrimaryButton
 */
interface PrimaryButtonProps {
	/**
	 * Usually a URL but you can use ${tokens} here too.
	 */
	target?: string,

	/**
	 * The label to display
	 */
	label?: string
}

/**
* A large, centered button.
*/
const PrimaryButton: React.FC<PrimaryButtonProps> = ({ label, target }) => {
	var href = useTokens(target, {});
	
	return <table role="presentation" cellSpacing={0} style={{ margin: "auto" }} cellPadding={0} border={0} align="center">
		<tbody>
			<tr>
				<td className="button-td button-td-primary" style={{borderRadius: "4px", background: "#222222"}}>
					 <a className="button-a button-a-primary" href={href} style={
						 {
							 background: "#222222",
							 border: "1px solid #000000",
							 fontFamily: "sans-serif",
							 fontSize: "15px",
							 lineHeight: "15px",
							 textDecoration: "none",
							 padding: "13px 17px",
							 color: "#ffffff",
							 display: "block",
							 borderRadius: "4px"
						}
					}>
						{label}
					</a>
				</td>
			</tr>
		</tbody>
	</table>;
}

export default PrimaryButton;