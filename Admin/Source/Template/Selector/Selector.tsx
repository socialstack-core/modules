import templateApi, { Template } from 'Api/Template';
import Input from 'UI/Input';
import { useState, useEffect } from 'react';

/**
 * Props for the Selector component.
 */
interface SelectorProps {
	/**
	 * Field name.
	 */
	name: string,

	/**
	 * Field label (if any)
	 */
	label?: React.ReactNode,

	/**
	 * Must be an admin template key.
	 */
	value?: string,

	/**
	 * Called when the selection is changed.
	 * @param selection
	 * @returns
	 */
	onChange?: (selection?: AdminTemplate) => void,

	/**
	 * Optional template type restriction. 1=web (pages), 2=email, 3= pdfs
	 */
	templateType?: uint
}

export type AdminTemplate = {
	/**
	 * A string which can be used to recover a full AdminTemplate object if needed.
	 */
	key: string,

	/**
	 * Nice name for the template.
	 */
	niceName: string,

	/**
	 * Set if it's a built in template (from code).
	 */
	componentName?: string,

	/**
	 * A module if it's a built in template.
	 */
	component?: any,
	/**
	 * A template object if it originated from the db.
	 */
	template?: Template,
};

/**
 * Use this to select a template which can also include code based templates. 
 * Emits an onChange event which indicates info about the template. 
 * @param props React props.
 */
const Selector: React.FC<SelectorProps> = (props) => {
	const { templateType } = props;
	const [templates, setTemplates] = useState<AdminTemplate[] | undefined>();

	useEffect(() => {
		templateApi.list(templateType ? {
			query: 'TemplateType=?',
			args: [templateType],
			pageSize: 200 as uint,
			pageIndex: 0 as uint
		} : undefined).then(apiTemplates => {

			const allTemplates: AdminTemplate[] = [];

			// Load all of the code ones as well (base templates)
			for (var moduleKey in window.__mm) {
				if (
					!moduleKey.startsWith('ui/templates/') &&
					!moduleKey.startsWith('admin/templates/') &&
					!moduleKey.startsWith('email/templates/')
				) {
					continue;
				}

				// Store base templates as the common AdminTemplate type
				allTemplates.push({
					key: "base:" + moduleKey,
					niceName: moduleKey.replace('/templates/', '/'),
					componentName: moduleKey,
					component: window.__mm[moduleKey]
				} as AdminTemplate);
			}

			// Convert the api templates in to the common AdminTemplate 
			// object as well which covers both types of template.
			apiTemplates.results.map(template => {
				allTemplates.push({
					key: "content:" + template.id,
					niceName: template.title || 'Template #' + template.id,
					template: template
				});
			});

			setTemplates(allTemplates);
		});
	}, [templateType]);

	const renderGroup = (label:string | undefined, filterFunc: (template:AdminTemplate) => boolean) => {
		const opts = templates!.filter(filterFunc).map(template => {
			return <option value={template.key}>
				{template.niceName}
			</option>
		});

		if (!label) {
			return opts;
		}

		return <optgroup label={label}>
			{opts}
		</optgroup>
	};

	return (
		<Input type='select' name={props.name} label={props.label} onChange={(e) => {
			const value = (e.target as HTMLSelectElement).value;
			let result: AdminTemplate | undefined;

			if (value) {
				result = templates?.find(template => template.key == value);
			}

			props.onChange && props.onChange(result);
		}}>
			<option value=''>{`None`}</option>
			{templates ? <>
				{!templateType && renderGroup(`Base Templates`, template => !!template.component)}
				{renderGroup(templateType ? undefined : `Editable Templates`, template => !!template.template)}
			</> : `Loading..`}
		</Input>
	);
}

export default Selector;