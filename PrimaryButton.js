export default class PrimaryButton extends React.Component {
	
	render(){
		var { origin } = global.location;
		var target = this.props.target;
		if(!target){target = '';}
		
		if(target.indexOf('http') != 0){
			target = origin + '/' + target;
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