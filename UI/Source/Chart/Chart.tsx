import {getUrl} from 'UI/FileRef';
import chartJsRef from './static/chart.js';
import { useRef, useEffect } from 'react';

var chartJsLoading: Promise<any> | undefined = undefined;

function loadChartJs(){
	if (chartJsLoading){
		return chartJsLoading;
	}
	
	return chartJsLoading = new Promise((success, reject) => {
		// Chart.js is lazy loaded. Go get it now:
		var script = document.createElement("script") as HTMLScriptElement;
		script.src = getUrl(chartJsRef)!;
		script.onload = () => {
			success((window as any).Chart);
		};
		script.onerror = reject;
		document.head.appendChild(script);
	});
}

type ChartProps = {
	/**
	 * Chart.js chart config.
	 */
	config: any
};

/**
 * The Chart React component.
 * @param props Chart props.
 */
const Chart: React.FC<ChartProps> = (props) => {
	const { config } = props;
	const chartRef = useRef<HTMLCanvasElement | null>(null);
	const chartInstanceRef = useRef<any>(null);

	useEffect(() => {
		let isMounted = true;

		loadChartJs().then((Chart: any) => {
			if (!isMounted || !chartRef.current) return;

			const ctx = chartRef.current.getContext("2d");
			if (!ctx) return;

			chartInstanceRef.current = new Chart(ctx, config);
		});

		return () => {
			isMounted = false;
			if (chartInstanceRef.current) {
				chartInstanceRef.current.destroy();
			}
		};
	}, [config]);

	return <canvas className="chartjs-chart" ref={chartRef} />;
};

export default Chart;