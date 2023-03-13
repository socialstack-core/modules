// gap support for flexbox wasn't introduced into Safari until v14.1 (v14.5 for iOS)
// this allows us to check if we need to add a fallback for the given browser
//
// e.g. assuming an <ul> of class "parent" with child "li.child" items:
//
// .parent {
//     display: flex;
//     gap: 1rem;
// }
//
// html.no-flexgap {
//     .parent .child {
//        margin-inline-start: .5rem; // NB: half original gap
//        margin-inline-end: .5rem;
//
//        &:first-child {
//          margin-inline-start: 0;
//        }
//
//        &:last-child {
//          margin-inline-end: 0;
//        }
//     }
// }
//
// NB: for flex children which wrap across several rows, use (in this example, each row contains 3 items):
//
// html.no-flexgap {
//     .parent .child {
//		  margin-block-end: 1rem;
//        margin-inline-end: 1rem;
//
//        &:nth-of-type(3n + 3) {
//          margin-inline-end: 0;
//        }
//     }
// }
function checkFlexGap() {
	if (window.SERVER) {
		return false;
    }

	// create flex container with row-gap set
	var flex = document.createElement("div");
	flex.style.display = "flex";
	flex.style.flexDirection = "column";
	flex.style.rowGap = "1px";

	// create two elements inside it
	flex.appendChild(document.createElement("div"));
	flex.appendChild(document.createElement("div"));

	// append to the DOM (needed to obtain scrollHeight)
	document.body.appendChild(flex);
	var isSupported = flex.scrollHeight === 1; // flex container should be 1px high from the row-gap
	flex.parentNode.removeChild(flex);

	return isSupported;
}

document.addEventListener("DOMContentLoaded", function() {
	var flexGapSupported = checkFlexGap();
	var html = window.SERVER ? undefined : document.querySelector("html");

	if (html) {

		if (flexGapSupported) {
			html.classList.remove("no-flexgap");
		} else {
			html.classList.add("no-flexgap");
		}

    }

});
