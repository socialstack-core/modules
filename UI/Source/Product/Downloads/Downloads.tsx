import { Product } from 'Api/Product';
import { Upload } from "Api/Upload";
import { getUrl } from "UI/FileRef";
import Link from "UI/Link";

/**
 * Props for the Downloads component.
 */
interface DownloadsProps {
	/**
	 * Optional section title to display above the downloads.
	 */
	title?: string;

	/**
	 * The product containing downloads to display.
	 */
	product: Product;

	/**
	 * A selected variant if any.
	 */
	currentVariant?: Product;
}

/**
 * The product downloads React component.
 * Renders a list of associated product download links.
 *
 * @param props React component props.
 */
const Downloads: React.FC<DownloadsProps> = ({ title, product, currentVariant }) => {
	const downloadsSource = currentVariant || product;
	const downloads = downloadsSource.productDownloads?.filter((download: Upload) => download.ref);

	if (!downloads?.length) {
		return;
	}

	return (
		<div className="ui-product-view__downloads">
			{/* Optional title header */}
			{title && (
				<h2 className="ui-product-view__subtitle">
					{title}
				</h2>
			)}

			{/* downloads */}
			<menu className="ui-product-view__downloads-list">
				{downloads.map((download: Upload) => {
					if (!download.ref) {
						return;
					}

					return <li>
						<Link external className="ui-product-view__downloads-file" href={getUrl(download.ref)}>
							{/* TODO: suffix with file type / size, e.g.:
							  * [icon] User Manual (PDF - 110Kb)
							  */}
							<i className="fr fr-download"></i>
							<span>
								{download.originalName}
							</span>
						</Link>
					</li>;
				})}
			</menu>
		</div>
	);
};

export type AllDownloadProps = {
	products: Product[];
}

const AllDownloads: React.FC<AllDownloadProps> = (props) => {
	
	const {products} = props;
	const downloads = products.flatMap((product: Product) => product.productDownloads);

	return (
		<div className="ui-product-view__downloads">
			
			{/* downloads */}
			<menu className="ui-product-view__downloads-list">
				{downloads.map((download?: Upload) => {
					if (!(download?.ref)) {
						return;
					}

					return <li>
						<Link external className="ui-product-view__downloads-file" href={getUrl(download.ref)}>
							{/* TODO: suffix with file type / size, e.g.:
							  * [icon] User Manual (PDF - 110Kb)
							  */}
							<i className="fr fr-download"></i>
							<span>
								{download.originalName}
							</span>
						</Link>
					</li>;
				})}
			</menu>
		</div>
	)
	
}

const COSHHDownloads: React.FC<DownloadsProps> = ({ title, product, currentVariant }) => {
	const downloads = product.coshhDocuments?.filter((download: Upload) => download.ref);

	if (!downloads?.length) {
		return;
	}

	return (
		<div className="ui-product-view__downloads">
			{/* Optional title header */}
			{title && (
				<h2 className="ui-product-view__subtitle">
					{title}
				</h2>
			)}

			{/* downloads */}
			<menu className="ui-product-view__downloads-list">
				{downloads.map((download: Upload) => {
					if (!download.ref) {
						return;
					}

					return <li>
						<Link external className="ui-product-view__downloads-file" href={getUrl(download.ref)}>
							{/* TODO: suffix with file type / size, e.g.:
							  * [icon] User Manual (PDF - 110Kb)
							  */}
							<i className="fr fr-download"></i>
							<span>
								{download.originalName}
							</span>
						</Link>
					</li>;
				})}
			</menu>
		</div>
	);
};

const AllCOSHHDownloads: React.FC<AllDownloadProps> = (props) => {

	const {products} = props;
	const downloads = products.flatMap((product: Product) => product.coshhDocuments);

	if (!downloads?.length) {
		return;
	}

	return (
		<div className="ui-product-view__downloads">
			
			{/* downloads */}
			<menu className="ui-product-view__downloads-list">
				{downloads.map((download?: Upload) => {
					if (!(download?.ref)) {
						return;
					}

					return <li>
						<Link external className="ui-product-view__downloads-file" href={getUrl(download.ref)}>
							{/* TODO: suffix with file type / size, e.g.:
							  * [icon] User Manual (PDF - 110Kb)
							  */}
							<i className="fr fr-download"></i>
							<span>
								{download.originalName}
							</span>
						</Link>
					</li>;
				})}
			</menu>
		</div>
	);
}

export default Downloads;

export {
	COSHHDownloads,
	AllDownloads,
	AllCOSHHDownloads
};