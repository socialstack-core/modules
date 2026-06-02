import { useState } from 'react';
import Button from 'UI/Button';
import Alert from 'UI/Alert';

/**
 * Props for the TestComponent component.
 */
interface TestComponentProps {
}

/**
 * This component displays a variety of common UI pieces to preview a theme with.
 * For example, primary/ secondary buttons, different size variants, alerts etc.
 * @param props React props.
 */
const TestComponent: React.FC<TestComponentProps> = (props) => {
	var [size, setSize] = useState<string>('');

	function renderButtons(size: string, outlined: boolean) {

		return <>
			{renderButton(size, outlined, "Primary")}
			{renderButton(size, outlined, "Secondary")}
			{renderButton(size, outlined, "Success")}
			{renderButton(size, outlined, "Danger")}
			{renderButton(size, outlined, "Warning")}
			{renderButton(size, outlined, "Info")}
			{renderButton(size, outlined, "Link")}
		</>;

	}

	function renderButton(size: string, outlined: boolean, variant: string) {

		if (variant == "Link") {
			return <>
				<Button href="#" xs={size == 'xs'} sm={size == 'sm'} lg={size == 'lg'} xl={size == 'xl'}>
					{variant}
				</Button>
			</>;
		}

		return <>
			<Button xs={size == 'xs'} sm={size == 'sm'} lg={size == 'lg'} xl={size == 'xl'} variant={variant.toLowerCase()} outlined={outlined}>
				{variant}
			</Button>
		</>;

	}

	function renderAlerts() {

		return <>
			<Alert variant="primary">
				{`A simple primary alert—check it out!`}
			</Alert>
			<Alert variant="secondary">
				{`A simple secondary alert—check it out!`}
			</Alert>
			<Alert variant="success">
				{`A simple success alert—check it out!`}
			</Alert>
			<Alert variant="danger">
				{`A simple danger alert—check it out!`}
			</Alert>
			<Alert variant="warning">
				{`A simple warning alert—check it out!`}
			</Alert>
			<Alert variant="info">
				{`A simple info alert—check it out!`}
			</Alert>
		</>;

	}

	return <div className="theme-test-component">
		<div>
			<select onChange={e => setSize(e.target.value)}>
				<option value='xs'>{`Extra Small`}</option>
				<option value='sm'>{`Small`}</option>
				<option value=''>{`Medium (default)`}</option>
				<option value='lg'>{`Large`}</option>
				<option value='xl'>{`Extra Large`}</option>
			</select>
		</div>
		{renderButtons(size, false)}
		<hr />
		{renderButtons(size, true)}
		<hr />
		{renderAlerts()}
	</div>;
}

export default TestComponent;