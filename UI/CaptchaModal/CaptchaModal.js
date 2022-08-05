import webRequest from 'UI/Functions/WebRequest';
import { useSession } from 'UI/Session';
import getRef from 'UI/Functions/GetRef'; 
import Modal from 'UI/Modal';
import Canvas from 'UI/Canvas';
import Html from 'UI/Html';

export default function CaptchaModal(props) {

	//access session info, such as the currently logged-in user:
	const { session , setSession} = useSession();
	const { visible, onClose , maxAttempts} = props;

	var [loading, setLoading] = React.useState();
	var [failure, setFailure] = React.useState();
	var [captcha, setCaptcha] = React.useState();	
	var [rawSvg, setRawSvg] = React.useState();
	var [tagZone, setTagZone] = React.useState();
	var [attempt, setAttempt] = React.useState();

	const closeIcon = <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20.953 20.953">
	<path data-name="Icon ionic-md-close" d="m20.954 2.096-2.1-2.1-8.377 8.381L2.096 0l-2.1 2.1 8.381 8.377L0 18.858l2.1 2.1 8.377-8.381 8.381 8.381 2.1-2.1-8.381-8.381Z" /></svg>;

	function getTagValue(e) {
		var tagValue = '';
		var node;
		
		if (e.target.tagName == "g") {
			node = e.target;
		}
		else if (e.target.parentNode) 
		{
			node = e.target.parentNode;
		}

		if (node) {
			if(node.tagName == "g" && node.id) {
				tagValue = node.id;
			}
			while (!tagValue && node.parentNode) {
				node = node.parentNode;
				if (node.tagName == "g" && node.id) {
					tagValue = node.id;
				}						
			}
		}

		return tagValue;
	}

	const onHoverGroup = (e) => {
		if (captcha && captcha.showDebug) {
			setTagZone(getTagValue(e));
		}
	}

	const onClickConfirm = (e) => {
		var tagValue = getTagValue(e);

		if (! tagValue) {
			return;
		}

		webRequest('captcha/check/' + captcha.id + '?tag=' + tagValue).then(response => {
			console.log('captcha check - canvote', response.json);
			if(response.json === true) {
				console.log('well done');
				// user clicked on the correct item
				// update session user with updated vote param
				session.user.canVote = true;
				setSession({...session});
				//close and pass back to allow next action to happen
				onClose();
			} else {
				// wrong item
				console.log('guess again');
				if(attempt >= maxAttempts) {
					onClose();
				}
				setAttempt(attempt + 1);			
			}
		}).catch(e => {
			console.log(e);
			setFailure(e);
		});
	}

	React.useEffect(() => {
		setAttempt(1);
		setLoading(true);
		webRequest('captcha/random/').then(response => {
			if(response.json.isActive) {
				setCaptcha(response.json);

				// get the raw svg so that we can inject it inline 
				var svgUrl = getRef(response.json.foregroundRef, {url: true});
				webRequest(svgUrl, null, {rawText: true}).then(svgText => {
					setRawSvg(svgText.text);
					setLoading(false);
				});
			} else {
				// no captchas so skip check
				// update session user with updated vote param
				session.user.canVote = true;
				setSession({...session});
				onClose();
			}
		}).catch(e => {
			console.log('Failed to locate captcha',e);
			setFailure(e);
			setLoading(false);
		});
	}, []);

	return (
		<Modal className="captcha-modal" visible={visible} onClose={onClose} closeIcon={closeIcon}>
			{failure &&
				<div className="register-error">
					<Alert type="error">{failure.message}</Alert>
				</div>	
			}

			{!loading && captcha && captcha.showDebug &&
				<div>
					DEBUG - Attempt {attempt} of {maxAttempts}
					<Html>{'&nbsp;' + tagZone }</Html>
				</div>
			}

			{!loading && captcha && rawSvg &&			
				<div className="captcha-modal__inner">
					<Canvas>{captcha.prompt}</Canvas>					
					<div className="captcha-modal___imgwrapper">
						<Html onMouseOver={e => { onHoverGroup(e) }} onClick={e => { onClickConfirm(e) }}>{rawSvg}</Html>

						<img className="imgBackGround" src={getRef(captcha.backgroundRef, { url: true})} />
					</div>
				</div>
			}	
		</Modal>
	);
}

CaptchaModal.propTypes = {
	maxAttempts: 'integer'
};

// use defaultProps to define default values, if required
CaptchaModal.defaultProps = {
	maxAttempts: 3
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
CaptchaModal.icon='user-alt-slash'; // fontawesome icon

