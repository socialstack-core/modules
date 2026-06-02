import Button from 'UI/Button';
import Modal from 'UI/Modal';
import Loading from 'UI/Loading';
import componentGroupApi from 'Api/ComponentGroup';
import { useState, useEffect } from 'react';

function formatTitle(name: string | undefined) {
	return name ? name.replace(/([a-z])([A-Z])/g, '$1 $2') : '';
}

type ComponentGroupResult = {
	id: uint;
	name?: string;
	key: string;
	[key: string]: any;
};

export default function ComponentGroupSelector(props: { selectOpenFor: boolean; onClose: () => void; onSelected: (group: ComponentGroupResult) => void }) {
	const { selectOpenFor, onClose, onSelected } = props;
	var [groups, setGroups] = useState<ComponentGroupResult[] | null>(null);
	
	useEffect(() => {
		if (selectOpenFor && !groups) {
			componentGroupApi.listAll().then((result: any) => {
				setGroups(result.results || []);
			});
		}
	}, [selectOpenFor, groups]);

	const renderModalContent = () => {
		if (!groups) {
			return <Loading />;
		}

		if (groups.length === 0) {
			return <p className="p-3 text-muted">{`No component groups available.`}</p>;
		}

		return (
			<div className="module-groups-wrapper">
				<div className="module-groups-filters p-3">
					<div className="module-group">
						<div className="module-group__internal">
							{groups.map(group => (
								<Button
									className="module-tile"
									key={group.id}
									onClick={() => {
										onSelected && onSelected(group);
										onClose && onClose();
									}}
								>
									<div className="module-tile__icon">
										<i className="fa fa-th-large" />
									</div>
									<div className="module-tile__content">
										<div className="module-tile__title">{formatTitle(group.name) || group.key}</div>
										<div className="module-tile__subtitle">{group.key}</div>
									</div>
								</Button>
							))}
						</div>
					</div>
				</div>
			</div>
		);
	};
	
	return (
		<Modal
			className="module-select-modal"
			buttons={[
				{
					label: `Close`,
					onClick: onClose
				}
			]}
			isLarge
			title={`Add Component Group`}
			onClose={onClose}
			visible={selectOpenFor}
		>
			{renderModalContent()}
		</Modal>
	);
}
