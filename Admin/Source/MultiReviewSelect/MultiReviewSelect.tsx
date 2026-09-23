import MultiSelect from 'Admin/MultiSelect';
import { Review } from 'Api/Review';
import { ListFilter } from 'Api/Startup';

export type MultiReviewSelectChangeEvent = {
	target: { value: number[] };
	fullValue: Review[];
};

export type MultiReviewSelectProps = {
	/** Current value — array of review objects (with `id`) or plain ID numbers. */
	value?: Review[] | number[];
	/** Initial value used when `value` is not provided. */
	defaultValue?: Review[] | number[];
	/** Hidden input name used for form submission. */
	name?: string;
	/** Label text shown above the review selector. */
	label?: string;
	/** If true the label is hidden. */
	hideLabel?: boolean;
	/** Optional help text. */
	help?: string;
	/** Maximum number of reviews that can be selected. */
	max?: number;
	/** Whether to show per-entry action buttons (edit). */
	showEntryActions?: boolean;
	/** Called when the selection changes. */
	onChange?: (e: MultiReviewSelectChangeEvent) => void;
	/** Called when the selection changes (raw variant). */
	onRawChange?: (e: MultiReviewSelectChangeEvent) => void;
};

/**
 * Renders a single review in the selected entries list and in search results.
 * Reviews have no name/title field, so the client name and trip name are shown
 * alongside a star rating instead.
 */
const ReviewEntry = ({ review }: { review: Review }) => {
	var rating = parseFloat(review.rating as any) || 0;
	var stars = [];

	for (var i = 0; i < 5; i++) {
		var isHalf = !Number.isInteger(rating) && i === Math.floor(rating);
		var isFull = i < Math.floor(rating);

		var starClass = isFull ? 'fas fa-star' : (isHalf ? 'fas fa-star-half-alt' : 'fal fa-star');
		var stateClass = isFull ? 'multi-review-select__star--full' : (isHalf ? 'multi-review-select__star--half' : 'multi-review-select__star--empty');

		stars.push(<i key={i} className={starClass + ' ' + stateClass}></i>);
	}

	return (
		<div className="multi-review-select__summary">
			<div className="multi-review-select__rating">
				{stars}
			</div>
			<div className="multi-review-select__who">
				<span className="multi-review-select__name">{review.clientName || `Review`}</span>
				{review.tripName && <span className="multi-review-select__trip">{review.tripName}</span>}
			</div>
		</div>
	);
};

/**
 * Builds a search filter which looks across the client name, trip name and rating.
 */
const buildReviewSearch = (query: string): ListFilter => {
	var q = '';
	var args: any[] = [];

	var fields = ['ClientName', 'TripName', 'Rating'];

	for (var i = 0; i < fields.length; i++) {
		q = q ? q + ' OR ' : q;
		q += fields[i] + ' contains ?';
		args.push(query);
	}

	return { query: q, args } as ListFilter;
};

/**
 * A first-party multi-select for reviews, wrapping the general purpose
 * Admin/MultiSelect with Review specific display and search configuration.
 */
const MultiReviewSelect = (props: MultiReviewSelectProps) => {
	return (
		<div className="multi-review-select">
			<MultiSelect<Review>
				contentType="Review"
				field="ClientName"
				displayField="ClientName"
				renderEntry={review => <ReviewEntry review={review} />}
				renderSearchResult={review => <ReviewEntry review={review} />}
				searchQuery={buildReviewSearch}
				value={(props.value as any)}
				defaultValue={(props.defaultValue as any)}
				name={props.name}
				label={props.label}
				hideLabel={props.hideLabel}
				help={props.help}
				max={props.max}
				showEntryActions={props.showEntryActions}
				onChange={props.onChange as any}
				onRawChange={props.onRawChange as any}
			/>
		</div>
	);
};

export default MultiReviewSelect;