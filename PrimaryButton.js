import applyTokens from 'Email/Functions/ApplyTokens';
import {SessionConsumer} from 'UI/Session';


/*
* A large, centered button.
* Target can include {context.tokens} where 
*/

export default class PrimaryButton extends React.Component {
	
	render(){
		return <SessionConsumer>
			{session => this.renderIntl(session)}
		</SessionConsumer>
	}

	renderIntl(session){
		var { origin } = global.location;
		var target = this.props.target;
		if(!target){target = '';}
		
		// Apply {tokens}:
		target = applyTokens(target, session);
		
		if(target.indexOf('http') != 0){
			if(target.length && target[0] == '/'){
				target = origin + target;
			}else{
				target = origin + '/' + target;
			}
		}
		
		return <table role="presentation" style="margin: auto;" cellspacing="0" cellpadding="0" border="0" align="center">
				<tbody>
					<tr>
						<td class="button-td button-td-primary" style="border-radius: 4px; background: #222222;">
							 <a class="button-a button-a-primary" href={target} style="background: #222222; border: 1px solid #000000; font-family: sans-serif; font-size: 15px; line-height: 15px; text-decoration: none; padding: 13px 17px; color: #ffffff; display: block; border-radius: 4px;">
								{this.props.label || 'Click here'}
							 </a>
						</td>
					</tr>
				</tbody>
		</table>;
		
	}
	
}

PrimaryButton.propTypes = {
	label: 'string',
	target: 'string'
};