import webRequest from 'UI/Functions/WebRequest';
import Loop from 'UI/Loop';
import { useSession } from 'UI/Session';
import { useState, useEffect } from 'react'; 
import Row from 'UI/Row';
import Col from 'UI/Column';

export default function DiscountList(props) {
	const { manageCouponUrl, hideDiscountPence, hideDiscountPercentage } = props;

	var [success, setSuccess] = React.useState();
	var [failed, setFailed] = React.useState();
	var [loading, setLoading] = React.useState();

	const removeDiscount = discount => {
		if (!discount) {
			return;
		}

		discount.isDisabled = true;

		setLoading(true);

		webRequest('discount/' + discount.id, discount).then(response => {
			setLoading(false);
		}).catch(e => {
			console.log(e);
			setFailure(e);
			setLoading(false);
		});
	}

	return (
		<div className="discount-list">
			<div className="discount-list--loop">
				<Loop 
					over="discount/list"
					filter={{where: {isDisabled: false}}}
					includes={["Product"]}
					raw
					asTable
					live
					orNone={() => <div className="No Coupons">
						No discounts created.
					</div>}
				>
				{
					[
						// Render Header
						results => {
							return <> 
								<th>Name</th>
								<th>Product</th>
								{!hideDiscountPercentage &&
									<th>% Discount</th>
								}
								{!hideDiscountPence &&
									<th>£ Discount</th>
								}
								<th></th>
							</>;
						},
						// Render Row
						(discount, index, resultsCount) => {
							return <>
								<td className="discount--info name">
									<a href={manageCouponUrl + "/" + discount.id}>{discount.name}</a>
								</td>
								<td className="discount--info product-name">
									{discount.product
										? discount.product.name
										: "Any"
									}
								</td>
								{!hideDiscountPercentage &&
									<td className="discount--info percentage">
										{discount.discountPercentage > 0
											? discount.discountPercentage
											: "NA"
										}
									</td>
								}
								{!hideDiscountPence &&
									<td className="discount--info pounds">
										{discount.discountPence > 0
											? (discount.discountPence / 100)
											: "NA"
										}
									</td>
								}
								<td>
									<button className="btn btn-danger" onClick={e => removeDiscount(discount)}>Remove</button>
								</td>
							</>;
						}
					]
					}
				</Loop>
			</div>

			<div className="new-discount-button">
				<a href='/coupon/create' className="btn btn-primary">Create new discount</a>
			</div>
		</div>
	);
}


DiscountList.propTypes = {
	manageCouponUrl: 'string',
	hideDiscountPence: 'bool',
	hideDiscountPercentage: 'bool',
};

// use defaultProps to define default values, if required
DiscountList.defaultProps = {
	manageCouponUrl: '/coupon'
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
DiscountList.icon='badge-dollar'; // fontawesome icon
