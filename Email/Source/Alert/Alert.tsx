import { AlertType } from 'UI/Alert';

/**
 * Props for the Alert component.
 * @icon fal fa-exclamation-circle
 * @description Displays an alert message with optional title and styling variants.
 */
interface AlertProps {
	variant?: AlertType;
	title?: string;
}

const EMAIL_VARIANT_STYLES: Record<AlertType, { color: string; backgroundColor: string; borderColor: string }> = {
	primary: { color: 'rgb(7.8,66,151.8)', backgroundColor: 'rgb(206.6,226,254.6)', borderColor: 'rgb(182.4,211.5,254.4)' },
	secondary: { color: 'rgb(64.8,70.2,75)', backgroundColor: 'rgb(225.6,227.4,229)', borderColor: 'rgb(210.9,213.6,216)' },
	success: { color: 'rgba(60, 60, 59, .9)', backgroundColor: '#EBF7ED', borderColor: 'rgba(55, 179, 74, 0.25)' },
	danger: { color: 'rgba(60, 60, 59, .9)', backgroundColor: '#FBEAEA', borderColor: 'rgba(218, 50, 43, 0.25)'  },
	warning: { color: 'rgba(60, 60, 59, .9)', backgroundColor: '#FFF8E7', borderColor: 'rgba(252, 184, 19, 0.25)' },
	info: { color: 'rgba(60, 60, 59, .9)', backgroundColor: '#E8F1F8', borderColor: 'rgba(27, 117, 187, .25)',  },
	light: { color: 'rgb(99.2,99.6,100)', backgroundColor: 'rgb(253.6,253.8,254)', borderColor: 'rgb(252.9,253.2,253.5)' },
	dark: { color: 'rgb(19.8,22.2,24.6)', backgroundColor: 'rgb(210.6,211.4,212.2)', borderColor: 'rgb(188.4,189.6,190.8)' }
};

const tableStyle: React.CSSProperties = {
	width: '100%',
	borderCollapse: 'collapse'
};

const cellBaseStyle: React.CSSProperties = {
	padding: '16px',
	borderRadius: '4px',
	fontFamily: 'Arial, sans-serif',
	fontSize: '14px',
	lineHeight: '20px'
};

const headingStyle: React.CSSProperties = {
	margin: '0 0 12px',
	fontSize: '18px',
	lineHeight: '24px',
	fontWeight: 600
};

const Alert: React.FC<React.PropsWithChildren<AlertProps>> = (props) => {
	const { variant = 'info', title, children } = props;
	const palette = EMAIL_VARIANT_STYLES[variant] ?? EMAIL_VARIANT_STYLES.info;

	const cellStyle: React.CSSProperties = {
		...cellBaseStyle,
		backgroundColor: palette.backgroundColor,
		border: `1px solid ${palette.borderColor}`,
		color: palette.color
	};

	const headingColorStyle: React.CSSProperties = {
		...headingStyle,
		color: palette.color
	};

	return (
		<table width="100%" cellPadding={0} cellSpacing={0} role="presentation" style={tableStyle}>
			<tbody>
				<tr>
					<td style={cellStyle}>
						{title && <h2 style={headingColorStyle}>{title}</h2>}
						{children}
					</td>
				</tr>
			</tbody>
		</table>
	);
};

export default Alert;
