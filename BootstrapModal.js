import { useState, useRef } from 'react';

const MODAL_PREFIX = 'modal';

/**
 * Bootstrap Modal component
 * @param {string} title		- modal title
 * @param {boolean} showClose	- set true to show close button within header
 */
export default function BootstrapModal(props) {
	const { title, showClose } = props;
	
    var modalClass = [MODAL_PREFIX];

	return (
		<div className={modalClass.join(' ')}>
		</div>
	);
}

BootstrapModal.propTypes = {
};

BootstrapModal.defaultProps = {
}

BootstrapModal.icon='align-center';
