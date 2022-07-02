const BREADCRUMB_PREFIX = 'breadcrumb';

/**
 * Bootstrap Breadcrumb component
 * @param {array} items	- array of breadcrumb items (each containing id / jsx / url / callback)
 */
export default function Breadcrumb(props) {
	const { items } = props;

	if (!items || items.length == 0) {
		return;
    }

	var breadcrumbClass = [BREADCRUMB_PREFIX];

	return (
		<nav aria-label="breadcrumb">
			<ol className={breadcrumbClass.join(' ')}>
				{
					items.map((item, index) => {
						var isActive = (index == items.length - 1);
						var breadcrumbItemClass = [BREADCRUMB_PREFIX + '-item'];

						if (isActive) {
							breadcrumbItemClass.push('active');
                        }

						return <>
							<li className={breadcrumbItemClass.join(' ')} key={item.id} aria-current={isActive ? 'page' : undefined}>
								{!active && item.url && <>
									<a href={item.url}>
										{item.jsx}
									</a>
								</>}

								{!active && item.callback && <>
									<button type="button" className="btn btn-link" onClick={() => item.callback}>
										{item.jsx}
									</button>
								</>}

								{active && <>
									{item.jsx}
								</>}

							</li>
						</>;
					})
				}
			</ol>
		</nav>
	);
}

Breadcrumb.propTypes = {
};

Breadcrumb.defaultProps = {
}

Breadcrumb.icon='align-center';
