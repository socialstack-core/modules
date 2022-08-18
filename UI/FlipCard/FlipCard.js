/* FlipCard
 * usage:
 * 
 * -- two-sided card, flips on hover (or tap on mobile)
 * <FlipCard>
 *     <>
 *         front content
 *     </>
 *     <>
 *         rear content
 *     </>
 * </FlipCard>
 * 
 * -- two-sided card, manual flip based on flag
 * <FlipCard className={flipFlag ? 'flip-card--flip' : undefined}>
 *     <>
 *         front content
 *     </>
 *     <>
 *         rear content
 *     </>
 * </FlipCard>
 * 
 * -- single-sided card
 * <FlipCard className={flipFlag ? 'flip-card--flip' : undefined}>
 *     content
 * </FlipCard>
 */

const FLIP_CARD_PREFIX = 'flip-card';

export default function FlipCard(props) {
	const { flipOnHover, flipVertical, className, children } = props;

	var flipClass = [FLIP_CARD_PREFIX];
	var hasFrontAndBack = Array.isArray(children) && children.length == 2;
	var frontChildren = hasFrontAndBack ? children[0] : children;
	var rearChildren = hasFrontAndBack ? children[1] : null;	 

	if (flipOnHover && hasFrontAndBack) {
		flipClass.push(FLIP_CARD_PREFIX + '--hover');
	}

	if (flipVertical) {
		flipClass.push(FLIP_CARD_PREFIX + '--vertical');
	}

	if (className) {
		flipClass.push(className);
    }

	return (
		<div className={flipClass.join(' ')}>
			<div class="flip-card__internal">
				<div class="flip-card__face flip-card__face--front">
					{frontChildren}
				</div>
				{hasFrontAndBack && <>
					<div class="flip-card__face flip-card__face--rear">
						{rearChildren}
					</div>
				</>}
			</div>
		</div>
	);
}

FlipCard.propTypes = {
	flipOnHover: 'boolean',
	flipVertical: 'boolean',
	children: true
};

FlipCard.defaultProps = {
	flipOnHover: true,
	flipVertical: false
}

FlipCard.icon='align-center';

