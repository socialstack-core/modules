import {useTokens} from 'UI/Token';
import webRequest from 'UI/Functions/WebRequest';
import Loop from 'UI/Loop';
import Spacer from 'UI/Spacer';

export default function ManageDiscount(props) {
	const { backButtonUrl } = props;

	var [failure, setFailure] = React.useState();
	var [loading, setLoading] = React.useState();
	var [discount, setDiscount] = React.useState();

	var discountId = useTokens('${url.id}');

	React.useEffect(() => {
		setLoading(true);

		webRequest('discount/list', {where: {id: discountId}}, {includes: ["Product"]}).then(response => {
			setDiscount(response?.json?.results?.at(0));
			setLoading(false);
		}).catch(e => {
			console.log(e);
			setFailure(e);
			setLoading(false);
		});
	}, []);

	const onClickGenerateCoupon = () => {
		setLoading(true);

		webRequest('coupon', {discountId: discountId}).then(response => {
			setLoading(false);
		}).catch(e => {
			console.log(e);
			setLoading(false);
			setFailure(e);
		});
	}

	var amountOffText = "";

	if (discount && discount.discountPercentage > 0) {
		amountOffText = discount.discountPercentage + "%";
	} else if (discount && discount.discountPence > 0) {
		amountOffText = "£" + (discount.discountPence / 100);
	}

	if (discount && discount.product) {
		amountOffText = amountOffText + " off " + discount.product.name;
	} else {
		amountOffText = amountOffText + " off any product";
	}
	
	return (
		<div className="coupons-manage-discount">
			{discount &&
				<center>
					<h4>{discount.name} ({amountOffText})</h4>
				</center>
			}
			<div className="coupons-manage-discount--coupons-loop">
				<Loop 
					over="coupon/list"
					filter={{where: {discountId: discountId, isDisabled: false}}}
					raw
					live
					orNone={() => <div className="No Coupons">
						No coupons generated.
					</div>}
				>
				{
					coupon => 
						{
							return <div className="coupons-manage-discount--coupon">
								<span className="coupon--info code">
									{coupon.code}
								</span>
								<span className={coupon.isRedeemed ? "coupon--info is-redeemed redeemed" : "coupon--info is-redeemed not-redeemed"}>
									{coupon.isRedeemed 
										? "Redeemed" 
										: "Not Redeemed"}
								</span>
							</div>;
						}
					}
				</Loop>
			</div>

			<div className="new-coupon-button">
				<button onClick={e => onClickGenerateCoupon()} className="btn btn-primary">Generate New Coupon</button>
			</div>
			<Spacer height="20"/>
			<div className="back-button">
				<a href={backButtonUrl} className="btn btn-primary">Back</a>
			</div>
		</div>
	);
}


ManageDiscount.propTypes = {
	backButtonUrl: 'string'
};

// use defaultProps to define default values, if required
ManageDiscount.defaultProps = {
	backButtonUrl: '/coupon'
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
ManageDiscount.icon='badge-dollar'; // fontawesome icon
