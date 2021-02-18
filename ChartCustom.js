import ChartJs from 'UI/Functions/Chart';
import prefersReducedMotion from 'UI/Functions/PrefersReducedMotion';

export default class ChartCustom extends React.Component {

	constructor(props) {
		super(props);
		this.canvasRef = React.createRef();
		this.chart = null;
		this.state = {
		}
		this.load = this.load.bind(this);
	}

	componentDidMount() {
		this.load(this.props);
	}

	isJson(str) {
		try {
			JSON.parse(str);
		} catch (e) {
			return false;
		}
		return true;
	}

	// draw arrow overlay on chart
	// ctx: chart context
	// data: chart data
	// fromColumn: index of column to start drawing arrow from
	// toColumn: index of column to start drawing arrow to
	// offset: use to allow 2 arrows to point to the same column without clashing
	// annotation: details of text to place next to arrow (object including 'alignRight' [boolean] and 'content' [array of string])
	drawArrow(ctx, data, fromColumn, toColumn, offset, annotation) {

		if (!data || !data.datasets || data.datasets.length == 0 || data.datasets[0].data.length < toColumn + 1) {
			return;
		}

		var modelFrom = data.datasets[data.datasets.length - 1]._meta[0].data[fromColumn]._model;
		var modelTo = data.datasets[data.datasets.length - 1]._meta[0].data[toColumn]._model;
		var arrowOffset = offset ? (modelTo.width / 3) : 0;
		var valueCount = 0;

		for (var i = 0; i < data.datasets[0].data.length; i++) {

			if (data.datasets[0].data[i] > 0) {
				valueCount++;
			}
		}

		var stacked = valueCount > 1;
		var arrowColour = stacked ? 'rgba(0,176,80,1)' : data.datasets[fromColumn].backgroundColor;

		// horizontal dotted line between columns
		ctx.beginPath();
		ctx.moveTo(modelFrom.x - (modelFrom.width / 2), modelFrom.y);
		ctx.lineTo(offset ? modelTo.x - (modelTo.width / 3) + 10 : modelTo.x + 10, modelFrom.y);
		ctx.lineWidth = 2;
		ctx.setLineDash([8, 4]);
		ctx.strokeStyle = arrowColour;
		ctx.stroke();

		// vertical dotted line down to top of column
		ctx.beginPath();
		ctx.moveTo(modelTo.x - arrowOffset, modelFrom.y);
		ctx.lineTo(modelTo.x - arrowOffset, modelFrom.y - (modelFrom.y - modelTo.y + 20));
		ctx.lineWidth = 20;
		ctx.setLineDash([]);
		ctx.strokeStyle = arrowColour;
		ctx.stroke();

		// arrowhead
		var path = new Path2D();
		path.moveTo(modelTo.x - arrowOffset - 20, modelFrom.y - (modelFrom.y - modelTo.y + 20));
		path.lineTo(modelTo.x - arrowOffset, modelFrom.y - (modelFrom.y - modelTo.y));
		path.lineTo(modelTo.x - arrowOffset + 20, modelFrom.y - (modelFrom.y - modelTo.y + 20));
		ctx.fillStyle = arrowColour;
		ctx.fill(path);

		if (typeof annotation === 'object' && annotation !== null) {

			if (annotation.alignRight) {
			}

			ctx.textAlign = 'left';
			ctx.textBaseline = 'bottom';

			var value;

			if (stacked) {
				var value1 = value2 = 0;

				for (var i = 0; i < data.datasets.length; i++) {
					value1 += data.datasets[i].data[0];
					value2 += data.datasets[i].data[1];
				}

				value = Math.ceil((value2 / value1) * 100);
			} else {
				value = data.datasets[fromColumn].data[fromColumn] - data.datasets[toColumn].data[toColumn];
			}

			ctx.fillStyle = arrowColour;

			var textX = modelTo.x - arrowOffset + 30;
			var textY = offset ? modelTo.y - 120 : modelTo.y - 40;

			if (annotation.content.length) {
				ctx.font = annotation.content.length > 2 ? "bold 32px Arial" : "normal 28px Arial";
				ctx.fillText(annotation.content[0].replace("[VALUE]", value), textX, textY);

				if (annotation.content.length > 2) {
					ctx.font = 'normal 13px Arial';
				}

				if (annotation.content.length > 1) {
					//ctx.fillText(annotation.content[1], modelTo.x - 150, modelFrom.y - 100);
					textY += annotation.content.length > 2 ? 16 : 30;
					ctx.fillText(annotation.content[1], textX, textY);
				}

				if (annotation.content.length > 2) {
					//ctx.fillText(annotation.content[2], modelTo.x - 150, modelFrom.y - 82);
					textY += annotation.content.length > 2 ? 20 : 12;
					ctx.fillText(annotation.content[2], textX, textY);
				}

			}

		}

	}

	load(props) {
		var {
			width, height, responsive, chartData,
			annotateColumn1to2, annotateColumn1to3, annotateColumn2to3
		} = props;

		const canvas = this.canvasRef.current;

		if (!canvas) {
			return;
		}

		const context = canvas.getContext("2d");

		chartData = chartData && this.isJson(chartData) ? JSON.parse(chartData) : {};
		chartData.options = chartData.options || {};
		chartData.options.responsive = responsive;
		chartData.options.maintainAspectRatio = true;
		chartData.options.aspectRatio = width / height;

		// ensure chart animation isn't triggered until the chart is visible
		// (see chartjs-plugin-deferred.js)
		chartData.options.plugins = chartData.options.plugins || {};
		chartData.options.plugins.deferred = chartData.options.plugins.deferred || {};
		chartData.options.plugins.deferred.yOffset = '50%'; // defer until 50% of the canvas height is inside the viewport
		//chartData.options.plugins.deferred.delay = 500;     // delay of 500 ms after the canvas is considered inside the viewport

		chartData.options.animation = {};

		var that = this;

		// arrow overlays
		if (annotateColumn1to2 != "" || annotateColumn1to3 != "" || annotateColumn2to3 != "") {

			chartData.options.animation.onComplete = function () {
				var ctx = this.chart.ctx;
				var data = this.data;
				debugger;
				if (annotateColumn1to2 != "") {
					that.drawArrow(ctx, data, 0, 1, false, {
						alignRight: true,
						content: annotateColumn1to2.split("\n")
					});
				}

				if (annotateColumn1to3 != "") {
					that.drawArrow(ctx, data, 0, 2, annotateColumn2to3 != "", {
						alignRight: false,
						content: annotateColumn1to3.split("\n")
					});
				}

				if (annotateColumn2to3 != "") {
					that.drawArrow(ctx, data, 1, 2, false, {
						alignRight: true,
						content: annotateColumn2to3.split("\n")
					});
				}

			};
		}

		chartData.options.animation.duration = prefersReducedMotion() ? 1 : 1000;

		var myChart = new ChartJs(context, chartData);
		//console.log("chart data: ", chartData);
	}

	render() {
		var { width, height, responsive } = this.props;

		return <div className="chart-custom">
			<canvas ref={this.canvasRef} width={!responsive ? width : undefined} height={!responsive ? height : undefined}></canvas>
		</div>;

	}

}

ChartCustom.propTypes = {
	width: 'int',
	height: 'int',
	responsive: 'bool',
	chartData: 'textarea',
	annotateColumn1to2: 'textarea',
	annotateColumn1to3: 'textarea',
	annotateColumn2to3: 'textarea'
};

ChartCustom.defaultProps = {
	width: 750,
	height: 400,
	responsive: true,
	chartData: '',
	annotateColumn1to2: '',
	annotateColumn1to3: '',
	annotateColumn2to3: ''
}

ChartCustom.icon = 'chart-area';
