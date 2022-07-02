import { TokenResolver } from 'UI/Token';

/*
* A large, centered button.
*/

export default function PrimaryButton (props) {
	
	return <TokenResolver value={props.target}>
		{target => <table role="presentation" cellspacing="0" style={{margin: "auto"}} cellpadding="0" border="0" align="center">
			<tbody>
				<tr>
					<td class="button-td button-td-primary" style={{borderRadius: "4px", background: "#222222"}}>
						 <a class="button-a button-a-primary" href={target} style={
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
							{props.label}
						</a>
					</td>
				</tr>
			</tbody>
		</table>}
	</TokenResolver>;
}

PrimaryButton.propTypes = {
	label: {type: 'jsx', default: `Click here`},
	target: 'token'
};

PrimaryButton.groups = 'email';