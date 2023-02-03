import webRequest from 'UI/Functions/WebRequest';
import Loop from 'UI/Loop';
import { useState, useEffect, useRef } from 'react';
import Container from "UI/Container";
import Row from "UI/Row";
import Col from "UI/Column";
import Form from "UI/Form";
import Input from "UI/Input";
import Validation from "UI/Function/Validation"
import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import getAutoForm from 'UI/CustomForm/GetAutoForm';
import {expand} from 'UI/Functions/CanvasExpand';
import Canvas from 'UI/Canvas';

export default function CustomForm(props) {
	// reference propTypes
	const { title, formIntroText, contentType, showLabel } = props;
	const [contentTypeInfo, setContentTypeInfo] = useState();
	const [fields, setFields] = useState();
	const [fieldsExpanded, setFieldsExpanded] = useState();
	const [isSent, setIsSent] = useState(false);
	const [isLoading, setIsLoading] = useState(false);
	const [failed, setFailed] = useState();
	const form = useRef(null);

	useEffect(() => {
		if (contentType && contentType > 0) {
			webRequest("customContentType/" + contentType).then(response => {
				if (response && response.json) {
					const contentTypeResponse = response.json;

					getAutoForm('content', (contentTypeResponse.name).toLowerCase()).then(formData => {
						if (!formData) {
							console.error("Could not generate form using type: " + contentTypeResponse.name);
							return;
						}
						
						// The fields of this content type are..
						var fields = JSON.parse(formData.canvas);
						
						if(Array.isArray(fields)){
							fields = {content: fields};
						}

						// Remove lists such as Tags[] and Roles[]
						fields.content = fields.content.filter(f => 
							!(f.valueType && f.valueType.length >= 2 && f.valueType.slice(-2) == "[]")
							&& f.data.name != "name"
						);

						var expanded = expand(fields, null);

						for(let i=0; i<fields.content.length; i++) {
							var field = fields.content[i];
							if (!showLabel && field.data.type != "checkbox") {
								// Swap label for a placeholder
								field.data.placeHolder = field.data.label;
								field.data.label = null;
							}

							// Validation should be configurable by user when creating the form fields
							//field.data.validate = ["Required"];
						}

						setContentTypeInfo(contentTypeResponse);
						setFields(fields);
						setFieldsExpanded(expanded);
					});

				} else {
					console.error("No customContentType found with id " + contentType);
				}
			}).catch(e => {
				console.error(e);
			});
		} else {
			console.error("No contentType provided");
		}
	}, []);

	if (!contentTypeInfo || !fields || !fieldsExpanded) {
		return null;
	}

	renderFormFields = () => {
		return <Canvas>
			{fieldsExpanded}
		</Canvas>
	}

	return (
		<div className="custom-form">
			<Form ref={form} autoComplete="off" action={"/v1/" + contentTypeInfo.name.toLowerCase()}
				onValues={values => {
					setIsLoading(true);
					setFailed(false);
					return values;
				}}
				onFailed={e => {
					console.error(e);
					setIsLoading(false);
					setIsSent(false);
					setFailed(e);
				}} 
				onSuccess={response => {
					setIsSent(true);
					setIsLoading(false);
					setFailed(false);
				}}
			>
				<h2>{title}</h2>
      			<p>{formIntroText}</p>
				{isSent
					? <p>Thank you for your submission!</p>
					: renderFormFields()
				}
				{isLoading &&
					<Loading />
				}
				{!isSent &&
					<Input className="btn btn-outline-primary" type="submit" label="SEND" disabled={isSent || isLoading} />
				}
				{failed &&
					<Alert type="error" message={failed.message ? failed.message : "Something went wrong, please try again later."} />
				}
			</Form>
		</div>
	);
}

CustomForm.propTypes = {
  title: "string", // text input
  formIntroText: "string",
  showLabel: 'bool',
  contentType: { 
	  type: "id", content: "CustomContentType", filter: 
		{ where: 
			{ 
				deleted: false, 
				isForm: true 
			} 
		} 
  },
};

// use defaultProps to define default values, if required
CustomForm.defaultProps = {
  title: "Talk to a Destination Specialist",
  formIntroText: "Copy that goes above the form"
};

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
CustomForm.icon = "align-center"; // fontawesome icon
