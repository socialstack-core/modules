export default class Centered extends React.Component {
	
	render(){
		return <table border="0" cellpadding="0" cellspacing="0" class="content" style="width: 100%; border-collapse: separate; border-spacing: 0;">
			<tbody>
				<tr>
					<td class="text-content" style="font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; color: #333333; font-size: 15px; font-weight: 400; line-height: 1.4; padding: 15px 5px;" align="center">
						{this.props.children}
					</td>
				</tr>
			</tbody>
		</table>;
	}
	
}

Centered.propTypes = {
	children: true
};