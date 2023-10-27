
function renderButtons(size, outlines){
	var baseClass = size ? ("btn " + size) : "btn";
	
	if(outlines){
		baseClass += " btn-outline-";
	}else{
		baseClass += " btn-";
	}
	
	return <>
		<button type="button" class={baseClass + "primary"}>Primary</button>
		<button type="button" class={baseClass + "secondary"}>Secondary</button>
		<button type="button" class={baseClass + "success"}>Success</button>
		<button type="button" class={baseClass + "danger"}>Danger</button>
		<button type="button" class={baseClass + "warning"}>Warning</button>
		<button type="button" class={baseClass + "info"}>Info</button>
		<button type="button" class={baseClass + "light"}>Light</button>
		<button type="button" class={baseClass + "dark"}>Dark</button>
		<button type="button" class={baseClass + "link"}>Link</button>
	</>;
}

/*
This component displays a variety of common UI pieces to preview a theme with.
For example, primary/ secondary buttons, different size variants, alerts etc.
*/
export default function TestComponent(props){
	var [size, setSize] = React.useState();
	
	return <div className="theme-test-component">
		<div>
			<select onChange={e => {
				setSize(e.target.value);
			}}>
				<option value=''>Default</option>
				<option value='btn-sm'>Small</option>
				<option value='btn-lg'>Large</option>
			</select>
		</div>
		{renderButtons(size)}
		<hr />
		{renderButtons(size, true)}
		<hr />
		<div class="alert alert-primary" role="alert">
		  A simple primary alert—check it out!
		</div>
		<div class="alert alert-secondary" role="alert">
		  A simple secondary alert—check it out!
		</div>
		<div class="alert alert-success" role="alert">
		  A simple success alert—check it out!
		</div>
		<div class="alert alert-danger" role="alert">
		  A simple danger alert—check it out!
		</div>
		<div class="alert alert-warning" role="alert">
		  A simple warning alert—check it out!
		</div>
		<div class="alert alert-info" role="alert">
		  A simple info alert—check it out!
		</div>
		<div class="alert alert-light" role="alert">
		  A simple light alert—check it out!
		</div>
		<div class="alert alert-dark" role="alert">
		  A simple dark alert—check it out!
		</div>
	</div>
	
}