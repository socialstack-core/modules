import {lazyLoad} from 'UI/Functions/WebRequest';
import jspdfRef from './static/jspdf.js';
import getRef from 'UI/Functions/GetRef';

var library = null;

/*
* A very light wrapper for jsPdf to allow it to work in offline/ self contained (app) contexts
* without bloating the project with its hundreds of k's on startup
*/
export function createPdf(options){
	// Create returns a promise which you must .then()
	if(!library){
		
		return lazyLoad(getRef(jspdfRef, {url:1}))
		.then(imported => {
			library = window.jspdf;
		})
		.then(() => {
			return new library.jsPDF(options);
		});
	
	}
	
	return Promise.resolve(new library.jsPDF(options));
}