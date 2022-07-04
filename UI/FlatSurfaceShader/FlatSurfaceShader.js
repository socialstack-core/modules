import FSS from 'UI/Functions/FlatSurfaceShader';

//------------------------------
// Mesh Properties
//------------------------------
var MESH = {
	width: 1.2,
	height: 1.2,
	depth: 10,
	segments: 16,
	slices: 8,
	xRange: 0.8,
	yRange: 0.1,
	zRange: 1.0,
	ambient: '#555555',
	diffuse: '#FFFFFF',
	speed: 0.002
};

//------------------------------
// Light Properties
//------------------------------
var LIGHT = {
	count: 2,
	xyScalar: 1,
	zOffset: 100,
	ambient: '#880066',
	diffuse: '#FF8800',
	speed: 0.001,
	gravity: 1200,
	dampening: 0.95,
	minLimit: 10,
	maxLimit: null,
	minDistance: 20,
	maxDistance: 400,
	autopilot: false,
	draw: true,
	bounds: FSS.Vector3.create(),
	step: FSS.Vector3.create(
	  Math.randomInRange(0.2, 1.0),
	  Math.randomInRange(0.2, 1.0),
	  Math.randomInRange(0.2, 1.0)
	)
};

export default class FlatSurfaceShader extends React.Component {
	
	constructor(props){
		super(props);
		this.onMouseMove = this.onMouseMove.bind(this);
		this.onClick = this.onClick.bind(this);
		this.onWindowResize = this.onWindowResize.bind(this);
		this.animate = this.animate.bind(this);
	}
	
	componentDidMount(){
		this.cancelled = false;
		//------------------------------
		// Global Properties
		//------------------------------
		this.now = this.start = Date.now();
		this.center = FSS.Vector3.create();
		this.attractor = FSS.Vector3.create();
		this.scene = new FSS.Scene();
		this.renderer = new FSS.CanvasRenderer(this.canvas);
		var container = this.container;
		this.renderer.setSize(container.offsetWidth, container.offsetHeight);
		
		// Let there be light!
		this.createMesh();
		this.createLights();
		this.resize(container.offsetWidth, container.offsetHeight);
		this.animate();
		
		window.addEventListener('resize', this.onWindowResize);
	}
	
	createMesh() {
		var {scene, renderer, mesh} = this;
		scene.remove(mesh);
		renderer.clear();
		var geometry = this.geometry = new FSS.Plane(MESH.width * renderer.width, MESH.height * renderer.height, MESH.segments, MESH.slices);
		var material = new FSS.Material(MESH.ambient, MESH.diffuse);
		var mesh = this.mesh = new FSS.Mesh(geometry, material);
		scene.add(mesh);
		
		// Augment vertices for animation
		var v, vertex;
		for (v = geometry.vertices.length - 1; v >= 0; v--) {
			vertex = geometry.vertices[v];
			vertex.anchor = FSS.Vector3.clone(vertex.position);
			vertex.step = FSS.Vector3.create(
				Math.randomInRange(0.2, 1.0),
				Math.randomInRange(0.2, 1.0),
				Math.randomInRange(0.2, 1.0)
			);
			vertex.time = Math.randomInRange(0, Math.PIM2);
		}
	}
	
	createLights() {
		var {scene, renderer} = this;
		var l, light;
		for (l = scene.lights.length - 1; l >= 0; l--) {
			light = scene.lights[l];
			scene.remove(light);
		}
		renderer.clear();
		for (l = 0; l < LIGHT.count; l++) {
			light = new FSS.Light(LIGHT.ambient, LIGHT.diffuse);
			light.ambientHex = light.ambient.format();
			light.diffuseHex = light.diffuse.format();
			scene.add(light);

			// Augment light for animation
			light.mass = Math.randomInRange(0.5, 1);
			light.velocity = FSS.Vector3.create();
			light.acceleration = FSS.Vector3.create();
			light.force = FSS.Vector3.create();
		}
	}

	resize(width, height) {
		var {renderer, center} = this;
		renderer.setSize(width, height);
		FSS.Vector3.set(center, renderer.halfWidth, renderer.halfHeight);
		this.createMesh();
	}

	animate() {
		if(this.cancelled){
			return;
		}
		this.now = Date.now() - this.start;
		this.update();
		this.renderEffect();
		requestAnimationFrame(this.animate);
	}

	update() {
		var {renderer, center, attractor, now, scene, geometry} = this;
		
		var ox, oy, oz, l, light, v, vertex, offset = MESH.depth/2;
		
		// Update Bounds
		FSS.Vector3.copy(LIGHT.bounds, center);
		FSS.Vector3.multiplyScalar(LIGHT.bounds, LIGHT.xyScalar);

		// Update Attractor
		FSS.Vector3.setZ(attractor, LIGHT.zOffset);

		// Overwrite the Attractor position
		if (LIGHT.autopilot) {
			ox = Math.sin(LIGHT.step[0] * now * LIGHT.speed);
			oy = Math.cos(LIGHT.step[1] * now * LIGHT.speed);
			FSS.Vector3.set(attractor,
			LIGHT.bounds[0]*ox,
			LIGHT.bounds[1]*oy,
			LIGHT.zOffset);
		}
		
		// Animate Lights
		for (l = scene.lights.length - 1; l >= 0; l--) {
			light = scene.lights[l];

			// Reset the z position of the light
			FSS.Vector3.setZ(light.position, LIGHT.zOffset);

			// Calculate the force Luke!
			var D = Math.clamp(FSS.Vector3.distanceSquared(light.position, attractor), LIGHT.minDistance, LIGHT.maxDistance);
			var F = LIGHT.gravity * light.mass / D;
			FSS.Vector3.subtractVectors(light.force, attractor, light.position);
			FSS.Vector3.normalise(light.force);
			FSS.Vector3.multiplyScalar(light.force, F);

			// Update the light position
			FSS.Vector3.set(light.acceleration);
			FSS.Vector3.add(light.acceleration, light.force);
			FSS.Vector3.add(light.velocity, light.acceleration);
			FSS.Vector3.multiplyScalar(light.velocity, LIGHT.dampening);
			FSS.Vector3.limit(light.velocity, LIGHT.minLimit, LIGHT.maxLimit);
			FSS.Vector3.add(light.position, light.velocity);
		}

		// Animate Vertices
		for (v = geometry.vertices.length - 1; v >= 0; v--) {
			vertex = geometry.vertices[v];
			ox = Math.sin(vertex.time + vertex.step[0] * now * MESH.speed);
			oy = Math.cos(vertex.time + vertex.step[1] * now * MESH.speed);
			oz = Math.sin(vertex.time + vertex.step[2] * now * MESH.speed);
			FSS.Vector3.set(vertex.position,
			MESH.xRange*geometry.segmentWidth*ox,
			MESH.yRange*geometry.sliceHeight*oy,
			MESH.zRange*offset*oz - offset);
			FSS.Vector3.add(vertex.position, vertex.anchor);
		}

		// Set the Geometry to dirty
		geometry.dirty = true;
	}

	
	renderEffect() {
		var { renderer, scene } = this;
		
		renderer.render(scene);
		
		// Draw Lights
		if (LIGHT.draw) {
			var l, lx, ly, light;
			for (l = scene.lights.length - 1; l >= 0; l--) {
				light = scene.lights[l];
				lx = light.position[0];
				ly = light.position[1];
				renderer.context.lineWidth = 0.5;
				renderer.context.beginPath();
				renderer.context.arc(lx, ly, 10, 0, Math.PIM2);
				renderer.context.strokeStyle = light.ambientHex;
				renderer.context.stroke();
				renderer.context.beginPath();
				renderer.context.arc(lx, ly, 4, 0, Math.PIM2);
				renderer.context.fillStyle = light.diffuseHex;
				renderer.context.fill();
			}
		}
	}
	
	componentWillUnmount(){
		this.cancelled = true;
		window.removeEventListener('resize', this.onWindowResize);
	}
	
	onClick(e) {
		var { renderer, center, attractor } = this;
		FSS.Vector3.set(attractor, e.x, renderer.height - e.y);
		FSS.Vector3.subtract(attractor, center);
		LIGHT.autopilot = !LIGHT.autopilot;
	}

	onMouseMove(e) {
		var { renderer, center, attractor } = this;
		FSS.Vector3.set(attractor, e.x, renderer.height - e.y);
		FSS.Vector3.subtract(attractor, center);
	}
	
	onWindowResize() {
		this.resize(this.container.offsetWidth, this.container.offsetHeight);
		this.renderEffect();
	}

	render(){
		
		return <div className="flatSurfaceShader" ref={c => this.container = c}
			onClick={this.onClick}
			onMouseMove={this.onMouseMove}>
		  <div className="effect" ref={c => this.output = c}>
			<canvas ref={c => this.canvas = c} />
		  </div>
		  <div className="ui">
		  {
			  this.props.children
		  }
		  </div>
		</div>;
		
	}
	
}

FlatSurfaceShader.propTypes = {
	children: true
};
