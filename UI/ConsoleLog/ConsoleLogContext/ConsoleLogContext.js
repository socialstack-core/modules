import {isoConvert} from 'UI/Functions/DateTools';
import { useEffect } from 'react';
const ConsoleLogs = React.createContext();
export { ConsoleLogs };

var pendingConsoleLogElements = [];
var consoleLogElements = [];
var consoleOverrider = {};

export function useConsoleLogs() {
    // returns {ConsoleLogItems, setConsoleLogItems}
    return React.useContext(ConsoleLogs);
}

export const Provider = (props) => {

  const [ConsoleLogItems, setConsoleLogItems] = React.useState([]);

    useEffect(() => {
      wrap();

      console.log('adding console monitor');
      setInterval(UpdateConsoleLogItems , 2000);
    }, []);


    window.onerror = (...args) => {
      console.error("onerror", args);
    };

    const formatDateTime = (date) => {
      date = isoConvert(date);

      return date.getHours().toString().padStart(2, "0") + "-" + 
        date.getMinutes().toString().padStart(2, "0") + "-" + 
        date.getSeconds().toString().padStart(2, "0") + ':' + 
        date.getMilliseconds().toString().padStart(3, "0");
    }

    const UpdateConsoleLogItems = () => {
      if(pendingConsoleLogElements.length > 0) {

        while (pendingConsoleLogElements.length > 0) {
              var item = pendingConsoleLogElements.shift();
              consoleLogElements.push(item);
        }

        while (consoleLogElements.length > 100) {
          consoleLogElements.shift();
        }

        setConsoleLogItems([...consoleLogElements]);

        //todo - send to a server ?
      }
    }

  /**
      * wrap console methods to get contents and display on component output
      * @todo features: clear, download
      * @todo format: table, dir?, trace
      * @todo other: time, group, assert, context, count, memory, profile ...?
      */
    function wrap () {

      for (const _method of ['log', 'info', 'error', 'warn', 'debug']) {
        consoleOverrider[_method] = console[_method];
        // eslint-disable-next-line no-loop-func
        console[_method] = function (...args) {
            pendingConsoleLogElements.push({ mode: _method, content: args, timeStamp:formatDateTime(new Date()) });
            consoleOverrider[_method](...args);
        }
      }

    }    

    return (
        <ConsoleLogs.Provider
            value={{
                ConsoleLogItems,
                setConsoleLogItems
            }}
        >
            {props.children}
        </ConsoleLogs.Provider>
    );
};
export const ConsoleContextContext = (props) => <ConsoleLogs.Context>{v => props.children(v.ConsoleLogItems, v.setConsoleLogItems)}</ConsoleLogs.Context>;