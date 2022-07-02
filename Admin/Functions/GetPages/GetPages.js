/*
* Gets the list of pages. At a minimum this is their url and id.
*/
export default function(){
	if(!global.pageRouter || !global.pageRouter.state){
		return null;
	}
	return global.pageRouter.state.pages;
}