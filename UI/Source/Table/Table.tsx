import Loop, { LoopProps, LoopStatus, LoopPageConfig } from 'UI/Loop';
import { AutoApi, ApiIncludes } from 'Api/ApiEndpoints';
import { Content, VersionedContent } from 'Api/Content';

/**
 * Props for the Table component.
 */
interface TableProps<T extends Content<uint>, I extends ApiIncludes> extends LoopProps<T, I> {
	/**
	 * Optionally used to render your table's header.
	 * @returns
	 */
	onHeader?: (results: T[] | null) => React.ReactNode,
	sticky?: boolean
}

/**
 * The Table React component. Each child function should return a <tr> with the desired column arrangement inside it.
 * @param props React props.
 */
const Table = <T extends VersionedContent<uint>, I extends ApiIncludes>(props: TableProps<T, I>) => {

	const {
		onHeader,
		className,
		sticky,
		...loopProps
	} = props;

	var tableClasses = ['table', 'ui-table'];

	if (sticky) {
		tableClasses.push('ui-table--sticky');
	}

	if (className?.length) {
		tableClasses.push(className);
	}

	return (
		<Loop {...loopProps} onLayout={(content: React.ReactNode, results: T[] | null, loopStatus: LoopStatus, paginator?: React.ReactNode, pageCfg?: LoopPageConfig) => {
			// Optionally use loopStatus to hide the header etc if it is actually empty/loading.

			const table = <table className={tableClasses.join(' ')}>
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