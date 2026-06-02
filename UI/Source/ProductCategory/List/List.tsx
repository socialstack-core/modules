import { ProductCategory } from "Api/ProductCategory";
import Signpost from 'UI/ProductCategory/Signpost';

/**
 * Props for the List component.
 */
interface ListProps
 {
	/**
	 * The categories to list. Must have included 'primaryUrl'.
	 */
	content?: ProductCategory[]
}

/**
 * The List React component.
 * @param props React props.
 */
const List: React.FC<ListProps> = (props) => {

	const { content } = props;

	if (!content) {
		return null;
	}

	return (
        <div className="ui-productcategory-list__wrapper">
            <ul className="ui-productcategory-list ui-productcategory-list--large-thumbs">
				{content.map((category: ProductCategory) => 
                    <li className="ui-productcategory-list__category">
						<Signpost content={category} />
                    </li>
				)}
            </ul>
        </div>
	);
}

export default List;