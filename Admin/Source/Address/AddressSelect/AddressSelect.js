import React, { useState } from 'react';
import MultiSelect from 'Admin/MultiSelect';
import Modal from 'UI/Modal';

export default function AddressSelect(props) {
	
	const renderEntry = (entry) => {
		const parts = [];
		
		if (entry.name) {
			parts.push(entry.name);
		}
		if (entry.line1) {
			parts.push(entry.line1);
		}
		if (entry.line2) {
			parts.push(entry.line2);
		}
		if (entry.line3) {
			parts.push(entry.line3);
		}
		if (entry.county) {
			parts.push(entry.county);
		}
		if (entry.postcode) {
			parts.push(entry.postcode);
		}

		return parts.length > 0 ? parts.join(', ') : (entry.name || `Address ${entry.id}`);
	};
	
	return (
		<>
			<MultiSelect
				contentType="Address"
				label={props.label || "Addresses"}
				{...props}
				field={"line1"}
				renderEntry={renderEntry}
				renderSearchResult={renderEntry}
			/>
		</>
	);
}
