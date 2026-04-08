import Loop, { LoopProps, LoopStatus, LoopPageConfig } from 'UI/Loop';
import { AutoApi, ApiIncludes } from 'Api/ApiEndpoints';
import { Content, VersionedContent } from 'Api/Content';

/**
 * Props for the Table component.
 */
interface TableProps<T extends Content<uint>, I extends ApiIncludes> extends LoopProps<T, I> {
	/**
	 * Optionally used to render a caption for the table
	 * @returns
	 */
	onCaption?: React.ReactNode,

	/**
	 * Optionally used to render your table's header.
	 * @returns
	 */
	onHeader?: (results: T[] | null) => React.ReactNode,

	/**
	 * Keep table header in view upon scrolling table out of viewport
	 */
	sticky?: boolean,

	/**
	 * Render table caption above table (renders below table by default)
	 */
	captionAbove?: boolean,

	/**
	 * True if the table should be the extra small style.
	 */
	xs?: boolean,

	/**
	 * True if the table should be the small style.
	 */
	sm?: boolean,

	/**
	 * True if the table should be the regular style.
	 */
	md?: boolean,

	/**
	 * True if the table should be the large style.
	 */
	lg?: boolean,

	/**
	 * True if the table should be the extra large style.
	 */
	xl?: boolean,

	/**
	 * set true to render paginator only (no overview)
	 */
	paginatorOnly?: boolean,

	/**
	 * set true to render overview only (no paginator)
	 */
	overviewOnly?: boolean,

	/** 
	 * set true to have paginator dock to bottom of parent
	 */
	dockBottom?: boolean
}

/**
 * The Table React component. Each child function should return a <tr> with the desired column arrangement inside it.
 * @param props React props.
 */
const Table = <T extends VersionedContent<uint>, I extends ApiIncludes>(props: TableProps<T, I>) => {

	const {
		onCaption,
		onHeader,
		className,
		sticky,
		captionAbove,
		xs, sm, md, lg, xl,
		...loopProps
	} = props;

	var tableClasses = ['table', 'ui-table'];

	if (sticky) {
		tableClasses.push('ui-table--sticky');
	}

	if (captionAbove) {
		tableClasses.push('ui-table--caption-above');
	}

	if (xs) {
		tableClasses.push("ui-table--xs");
	}

	if (sm) {
		tableClasses.push("ui-table--sm");
	}

	if (md) {
		tableClasses.push("ui-table--md");
	}

	if (lg) {
		tableClasses.push("ui-table--lg");
	}

	if (xl) {
		tableClasses.push("ui-table--xl");
	}

	if (className?.length) {
		tableClasses.push(className);
	}

	return (
		<Loop {...loopProps} onLayout={(content: React.ReactNode, results: T[] | null, loopStatus: LoopStatus, paginator?: React.ReactNode, pageCfg?: LoopPageConfig) => {
			// Optionally use loopStatus to hide the header etc if it is actually empty/loading.

			const table = <table className={tableClasses.join(' ')}>
				{onCaption && <caption>
					{onCaption()}
				</caption>}
				{onHeader && <thead>
					{onHeader(results)}
				</thead>}
				<tbody>
					{content}
				</tbody>
			</table>;

			if (paginator) {
				// pageCfg always set in this scenario.
				return <>
					{pageCfg?.top && paginator}
					{table}
					{pageCfg?.bottom !== false && paginator}
				</>
			}

			return table;
		}}>
			{loopProps.children}
		</Loop>
	);
}

export default Table;