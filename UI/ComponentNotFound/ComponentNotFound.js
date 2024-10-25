import Alert from 'UI/Alert';

export default function ComponentNotFound(props) {
	const { notFoundMessage, notFoundStack } = props;

	return <Alert type='error'>
		{notFoundMessage}
		<details>
			<summary>{`Error details`}</summary>
			{notFoundStack}
		</details>
	</Alert>;
}
