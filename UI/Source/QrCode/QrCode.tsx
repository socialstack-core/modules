import {lazyLoad} from 'UI/Functions/WebRequest';
import {getUrl} from 'UI/FileRef';
// @ts-ignore
import qrRef from './static/qr.js';
import { useRef, useEffect } from 'react';

type QrCodeProps = {
	/** The text/URL to encode in the QR code. */
	text: string;
	/** Width of the QR code in pixels. Defaults to 128. */
	width?: number;
	/** Height of the QR code in pixels. Defaults to 128. */
	height?: number;
	/** Colour used for the dark modules. Defaults to "#000000". */
	dark?: string;
	/** Colour used for the light modules. Defaults to "#ffffff". */
	light?: string;
};

// Uses https://github.com/davidshimjs/qrcodejs (the imported module)
export default function QrCode(props: QrCodeProps) {
	const divRef = useRef<HTMLDivElement>(null);

	useEffect(() => {
		lazyLoad(getUrl(qrRef as string)!).then((exp : any) => {
			var div = divRef.current;

			if (!div) {
				return;
			}

			while (div.firstChild) {
				div.removeChild(div.firstChild);
			}

			var qrcode = new exp.QRCode(div, {
				text: props.text,
				width: props.width || 128,
				height: props.height || 128,
				colorDark: props.dark || "#000000",
				colorLight: props.light || "#ffffff",
				correctLevel: exp.QRCode.CorrectLevel.H
			});
		});
	}, [props.text, props.width, props.height, props.dark, props.light]);

	return (<div ref={divRef}></div>);
}