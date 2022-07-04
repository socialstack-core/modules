
/*
Note about the type prop: You *must* use a constant string.
UI/Icon is a first-class component in the compile process.
The compiler will use "type" to identify in-use icons and strip accordingly.

<Icon type="bullhorn" />
<Icon type="bullhorn" light />
*/
export default function Icon(props){
	const {type, count, light, solid, duotone, regular, brand, fixedWidth } = props;
	
	var variant = 'fa';
	
	if(light){
		variant = 'fal';
	}else if(duotone){
		variant = 'fad';
	}else if(brand){
		variant = 'fab';
	}else if(regular){
		variant = 'far';
	}else if(solid){
		variant = 'fas';
	}
	
	var className = variant + " fa-" + type;

	if (fixedWidth) {
		className += " fa-fw";
    }
	
	if(props.count){
		return  <span className="fa-layers fa-fw">
			<i className={className}></i>
			<span className="fa-layers-counter">{count}</span>
		  </span>;
	}
	
	return <i className={className} />;
}