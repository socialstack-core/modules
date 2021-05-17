import applyTokens from 'Email/Functions/ApplyTokens';
import Content from 'UI/Content';
import { useSession } from 'UI/Session';

/*
* A large, centered button.
* Target can include {context.tokens} where 
*/

export default function PrimaryButton (props) {
	
	var {session} = useSession();
	
	return <Content primary>
		{content => {
			
			var { origin } = global.location;
			var target = props.target;
			if(!target){target = '';}
			
			// Apply {tokens}:
			target = applyTokens(target, session, content);
			
			if(target.indexOf('http') != 0){
				if(target.length && target[0] == '/'){
					target = origin + target;
				}else{
					target = origin + '/' + target;
				}
			}
			
			return <table role="presentation" style={{margin: "auto"}} cellspacing="0" cellpadding="0" border="0" align="center">
					<tbody>
						<tr>
							<td class="button-td button-td-primary" style={{borderRadius: "4px", background: "#222222"}}>
								 <a class="button-a button-a-primary" href={target} style={{background: "#222222", border: "1px solid #000000", fontFamily: "sans-serif", fontSize: "15px", lineHeight: "15px", textDecoration: "none", padding: "13px 17px", color: "#ffffff", display: "block", borderRadius: "4px"}}>
									{props.label || 'Click here'}
								 </a>
							</td>
						</tr>
					</tbody>
			</table>;
		}}
	</Content>;
	
}

PrimaryButton.propTypes = {
	label: 'string',
	target: 'string'
};