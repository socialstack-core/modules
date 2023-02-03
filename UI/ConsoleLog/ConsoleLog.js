import Output from './Output'
import { useConsoleLogs } from './ConsoleLogContext'; 

var localHistory=[];

export default function ConsoleLog(props) {

	const {ConsoleLogItems, setConsoleLogItems} = useConsoleLogs();

return (
  <Output history={ConsoleLogItems} input={props.input} theme={props.theme} />
 )



};

