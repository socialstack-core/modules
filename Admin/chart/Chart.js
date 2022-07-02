import ChartJs from 'Admin/Functions/Chart';
import webRequest from "UI/Functions/WebRequest";
import formatTime from "UI/Functions/FormatTime";

const epoch = Date.UTC(2020,0,1,0,0,0);

// Uses https://github.com/chartjs/Chart.js (the imported module)
export default class Chart extends React.Component{
    constructor(props){
		super(props);
        this.canvasRef = React.createRef();
        this.load(props);
        this.state = {
            metrics : []
        }
    }
    
	getRange(sourceId){
		var range = 672; // Set to one week for now.
		var sourceId = parseInt(sourceId);
		
		// metric epoch time:
		var timestamp = Date.now() - epoch;
		
        var max = Math.floor(timestamp/(60 * 1000 * 15));
        var min = Math.floor(max - range);
		
		max |= (sourceId << 21);
		min |= (sourceId << 21);
		
		return {
			min, max
		};
	}
	
    load(props){
        var range = this.getRange(props.sourceId);
        webRequest('metricmeasurement/list', {where:[
			{
				Id: {
					geq: range.min
				}
			},
			{
				and: true,
				Id: {
					leq: range.max
				}
			}
		]}).then(response => {
            this.setState({
				error: null,
				metrics: response.json.results
            }, () => {
				this.redraw(this.props);
			});
		}).catch(e => {
			console.error(e);
			
			this.setState({
				metrics: null,
				error: e
			});
        });
    }


	componentDidMount(){
		this.redraw(this.props);
	}
	
	componentWillReceiveProps(props){
		this.redraw(props);
	}
	
	redraw(props){
        var range = this.getRange(props.sourceId);
        var results = this.state.metrics;

        var metrics = {};
		
		// create an id lookup:
        for(var i in results){
            metrics[results[i].id + ''] = results[i];
        }

		console.log(metrics, range);

        var chartData = [];
        var chartLabels = [];
		
        for(var metricId = range.min; metricId <= range.max; metricId++){
            // is there a value for this time slot?
            if(metrics[metricId + '']){
                // Yep, let's add to the data array and the label array.
                chartData.push(metrics[metricId + ''].count);
                chartLabels.push(convertTimestamp(metricId));
            }
            else{ 
                // Nope, generate the label from the current metricId 
                chartData.push(0);
                chartLabels.push(convertTimestamp(metricId));
            }
            
        }
		
        const canvas = this.canvasRef.current;
        const context = canvas.getContext("2d");

        var myChart = new ChartJs(context, {
            type: 'line',
            data: {
                labels: chartLabels,
                datasets:[{
                    label: 'Occurrences',
                    data: chartData,
                    backgroundColor: "rgba(0,123,255, .1)",
                    borderColor:'rgb(0,123,255)', 
                    borderWidth: 1
                }]
            },
            options: {
				elements: {
					point: {
						radius: 0
					}
				},
                scales: {
                    yAxes: [{
                        ticks: {
                            beginAtZero: true
                        }
                    }],
					xAxes: [{
                        ticks: {
                            autoSkip: true,
							maxTicksLimit: 20
                        }
                    }]
                }
            }
        });
    }

    render(){
		return (
            <div>
                <div className ="chart-container">
                    <canvas ref={this.canvasRef}></canvas>
                </div>
            </div>
        );	
    }
}

Chart.propTypes = {
	sourceId: 'int'
};

function convertTimestamp(sourceId){
    realTimestamp = ((sourceId & ((1<<21)-1)) * (15 * 60 * 1000)) + epoch;
    realDate = new Date(realTimestamp);
    return(formatTime(realDate));
}