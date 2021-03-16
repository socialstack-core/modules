interface ColumnProps {
	
	className? : string;
	size?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeXs?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeSm?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeMd?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeLg?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	sizeXl?: "auto" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offset?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offsetXs?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offsetSm?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offsetMd?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	offsetLg?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	
	/**
		An amount, in bootstrap grid columns, to offset your column by.
	*/
	offsetXl?: "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "10" | "11" | "12";
	
	/**
		Optionally add a specific type of margin to your column. See the bootstrap docs for what the available options do.
	*/
	margin?: "" | "ml-auto" | "mr-auto" | "mx-auto";
	
	/**
		Set this to true if you don't want gutters on your column.
	*/
	noGutters?: boolean;

}

/**
	A column for a flexbox based bootstrap grid. Use inside a Row.
*/
export default function Column(
    props: React.PropsWithChildren<ColumnProps>
): React.ReactElement;
