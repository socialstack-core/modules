/**
 * Props for the Centered component.
 * @icon fal fa-align-center
 * @description Centers content horizontally within an email.
 */
interface CenteredProps {
}

const Centered: React.FC<React.PropsWithChildren<CenteredProps>> = props => {
	
	return <table border={0} cellPadding={0} cellSpacing={0} className="content" style={{
		width: "100%",
		borderCollapse: "separate",
		borderSpacing: "0"
	}}>
		<tbody>
			<tr>
				<td className="text-content" style={{
					fontFamily: "&quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif",
					color: "#333333",
					fontSize: "15px",
					fontWeight: "400",
					lineHeight: "1.4",
					padding: "15px 5px"
				}} align="center">
					{props.children}
				</td>
			</tr>
		</tbody>
	</table>;
	
}

export default Centered;