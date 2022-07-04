import ChartJs from 'Admin/Functions/Chart';
import webRequest from 'UI/Functions/WebRequest';
import {ordinal} from "UI/Functions/DateTools";

const msPerSlice=(15 * 60 * 1000);
var epoch = new Date(2020, 0, 1).valueOf();

function toDate(sliceId){
    var realDate = new Date(epoch + (sliceId * msPerSlice));
    return realDate;
}

function toSlice(date){
	return Math.floor((date.valueOf() - epoch) / msPerSlice);
}

export default class HuddleMetricView extends React.Component {
	constructor(props){
		super(props);
        this.canvasRef = React.createRef();
        this.load(props);
        this.state = {
            metrics : []
        }
    }
    
    load(props){
		var now = new Date();
        var min = toSlice(now);
		var max = min + (4 * 24 * 7); // 4 slices per hour (7 days)
		
		var where = [
			{
				TimeSliceId: {greaterThan: min}
			},
			{
				and: true,
				TimeSliceId: {lessThan: max}
			}
		];
		
        webRequest('huddleloadmetric/list', {where}).then(response => {
			
			var { results } = response.json; 
			
			// group by server:
			var serversById = {};
			
			var minPoint = 0;
			var maxPoint = 0;
			
			for(var i=0;i<results.length;i++){
				var point = results[i];
				var id = point.huddleServerId+'';
				
				if(i==0){
					minPoint = maxPoint = point.timeSliceId;
				}else{
					if(point.timeSliceId < minPoint){
						minPoint = point.timeSliceId;
					}
					if(point.timeSliceId > maxPoint){
						maxPoint = point.timeSliceId;
					}
				}
				
				if(serversById[id]){
					serversById[id].points.push(point);
				}else{
					serversById[id]={
						id,
						points:[point]
					};
				}
			}
			
			var servers = [];
			
			for(var huddleServerId in serversById){
				var server = serversById[huddleServerId];
				// index the points by time slice:
				server.ptIndex = {};
				
				server.color='rgba(' + (Math.random() * 255) + ',' + (Math.random() * 255) + ',' + (Math.random() * 255) + ',1)';
				
				for(var i=0;i<server.points.length;i++){
					var point = server.points[i];
					server.ptIndex[point.timeSliceId+'']=point;
				}
				
				servers.push(server);
			}
			
			// Pad the points such that they always run from minPoint to maxPoint, with 0's:
			for(var i=0;i<servers.length;i++){
				var server = servers[i];
				var minMax = [];
				
				for(var metricId = minPoint; metricId <= maxPoint; metricId++){
					var point = server.ptIndex[metricId+''];
					minMax.push(point ? point.loadFactor : 0);
				}
				server.pointSet = minMax;
			}
			
			var info ={
				error: null,
				servers,
				minPoint,
				maxPoint
            };
			
            this.setState(info);
            this.rebuild(info);
		}).catch(e => {
			console.error(e);
			
			this.setState({
				metrics: null,
				error: e
			});
        });
    }

	rebuild(info){
        var {servers, minPoint, maxPoint} = info;
		
		if(!servers){
			return;
		}
		
		var datasets=[];
		var labels=[];
		
		for(var metricId = minPoint; metricId <= maxPoint; metricId++){
			labels.push(toDate(metricId));
		}
		
		for(var i=0;i<servers.length;i++){
			var server = servers[i];
			
			datasets.push({
				fill: false,
				label: server.id,
				data: server.pointSet,
				borderColor: server.color
			});
		}
		
        const canvas = this.canvasRef.current;
        const context = canvas.getContext("2d");

        var myChart = new ChartJs(context, {
            type: 'line',
            data: {
                labels,
                datasets
            },
            options: {
                scales: {
					xAxes: [{
						type: 'time',
                        ticks: {
							autoSkip: true,
							maxTicksLimit: 100
						}
					}],
                    yAxes: [{
                        ticks: {
                            beginAtZero: true
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

HuddleMetricView.propTypes={};
