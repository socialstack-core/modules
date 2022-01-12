import webRequest from 'UI/Functions/WebRequest';
import Loop from 'UI/Loop';
import { useSession } from 'UI/Session';
import { useState, useEffect } from 'react'; 
import Row from 'UI/Row';
import Col from 'UI/Column';

export default function DiscountList(props) {
	const { manageCouponUrl } = props;

	return (
		<div className="discount-list">
			<div className="discount-list--loop">
				<Loop 
					over="discount/list" 
					raw
				>
				{
					discount => 
						{
							return <div className="discount-list--discount">
								<span className="discount--info name">
									<a href={manageCouponUrl + "/" + discount.id}>{discount.name}</a>
								</span>
							</div>;
						}
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
	manageCouponUrl: 'string'
};

// use defaultProps to define default values, if required
DiscountList.defaultProps = {
	manageCouponUrl: '/coupon'
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
DiscountList.icon='badge-dollar'; // fontawesome icon
