/**
 * Shared inline styles for email components.
 * These styles are optimized for email client compatibility.
 */

export const emailStyles = {
	// Typography
	fontFamily: 'Arial, Helvetica, sans-serif',
	fontSize: {
		small: '12px',
		normal: '14px',
		large: '16px',
		heading: '18px'
	},
	colors: {
		text: '#333333',
		textLight: '#666666',
		border: '#e0e0e0',
		borderDark: '#333333'
	},

	// Common element styles
	table: {
		width: '100%',
		borderCollapse: 'collapse' as const,
		fontFamily: 'Arial, Helvetica, sans-serif',
		fontSize: '14px'
	},

	tableWithMargin: {
		width: '100%',
		borderCollapse: 'collapse' as const,
		fontFamily: 'Arial, Helvetica, sans-serif',
		fontSize: '14px',
		marginTop: '20px'
	},

	row: {
		borderBottom: '1px solid #e0e0e0'
	},

	cell: {
		padding: '10px 5px',
		verticalAlign: 'top' as const
	},

	cellHalf: {
		padding: '10px 5px',
		verticalAlign: 'top' as const,
		width: '50%'
	},

	cellLeft: {
		padding: '10px 5px',
		textAlign: 'left' as const
	},

	cellRight: {
		padding: '10px 5px',
		textAlign: 'right' as const,
		whiteSpace: 'nowrap' as const
	},

	heading: {
		margin: '0 0 10px 0',
		fontSize: '16px',
		fontWeight: 'bold' as const,
		fontFamily: 'Arial, Helvetica, sans-serif',
		color: '#333333'
	},

	// Address specific
	address: {
		fontStyle: 'normal' as const,
		lineHeight: '1.6',
		fontSize: '14px',
		fontFamily: 'Arial, Helvetica, sans-serif',
		color: '#333333'
	},

	addressLine: {
		display: 'block' as const,
		margin: '0',
		padding: '0'
	},

	// Container
	container: {
		width: '100%',
		marginTop: '20px',
		marginBottom: '20px'
	},

	// Main view container
	viewContainer: {
		width: '100%',
		maxWidth: '600px',
		margin: '0 auto',
		fontFamily: 'Arial, Helvetica, sans-serif',
		fontSize: '14px',
		color: '#333333',
		backgroundColor: '#ffffff',
		padding: '20px'
	},

	// Title/Heading styles
	pageTitle: {
		fontSize: '18px',
		fontWeight: 'bold' as const,
		margin: '0 0 10px 0',
		color: '#333333',
		fontFamily: 'Arial, Helvetica, sans-serif'
	},

	// Badge/Reference styles
	badge: {
		display: 'inline-block' as const,
		backgroundColor: '#f5f5f5',
		color: '#666666',
		padding: '4px 12px',
		borderRadius: '4px',
		fontSize: '14px',
		marginLeft: '10px',
		fontWeight: 'normal' as const
	},

	// Section container
	section: {
		marginBottom: '20px'
	},

	// Product table styles
	productTable: {
		width: '100%',
		borderCollapse: 'collapse' as const,
		fontFamily: 'Arial, Helvetica, sans-serif',
		fontSize: '14px',
		marginTop: '20px',
		border: '1px solid #e0e0e0'
	},

	productTableHeader: {
		padding: '12px 8px',
		backgroundColor: '#f5f5f5',
		borderBottom: '2px solid #e0e0e0',
		textAlign: 'left' as const,
		fontSize: '12px',
		fontWeight: 'bold' as const,
		fontFamily: 'Arial, Helvetica, sans-serif',
		color: '#333333'
	},

	productTableHeaderRight: {
		padding: '12px 8px',
		backgroundColor: '#f5f5f5',
		borderBottom: '2px solid #e0e0e0',
		textAlign: 'right' as const,
		fontSize: '12px',
		fontWeight: 'bold' as const,
		fontFamily: 'Arial, Helvetica, sans-serif',
		color: '#333333'
	},

	productTableCell: {
		padding: '12px 8px',
		borderBottom: '1px solid #e0e0e0',
		fontSize: '14px',
		fontFamily: 'Arial, Helvetica, sans-serif',
		color: '#333333',
		verticalAlign: 'middle' as const
	},

	productTableCellRight: {
		padding: '12px 8px',
		borderBottom: '1px solid #e0e0e0',
		fontSize: '14px',
		fontFamily: 'Arial, Helvetica, sans-serif',
		color: '#333333',
		verticalAlign: 'middle' as const,
		textAlign: 'right' as const
	},

	// Grand total row style
	grandTotalRow: {
		borderBottom: '1px solid #e0e0e0',
		borderTop: '2px solid #333',
		fontWeight: 'bold' as const,
		fontSize: '16px'
	}
} as const;
