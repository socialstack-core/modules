import {useEffect, useState, useCallback, useRef} from 'react';
import Tile from 'Admin/Tile';
import Alert from 'UI/Alert';
import Input from 'UI/Input';
import StdOutApi from 'Api/StdOutController';
import { isoConvert } from 'UI/Functions/DateTools';
import Debounce from "UI/Functions/Debounce";

declare global {
    var scrollMaxY: number;
}

export type StdOutMessage = {
    entry: string;
    trace?: string;
};

export type StdEntry = {
    createdUtc: Date;
    messages: StdOutMessage[];
    tag: string;
    type: string;
};

/**
 * This component is responsible for showing the output of Stdout. The
 * new updates to it mean we can nicely filter out unnecessary messages or 
 * any bloat getting in the way, as an additional helper to development,
 * any paths that are identified are now highlighted ,you can one click
 * copy then paste them into your IDE to go straight to the place where
 * the issue/log occured. 
 */
const StdOut: React.FC<{}> = (): React.ReactNode => {
    
    // holds the log, fairly primitive. 
    const [log, setLog] = useState<StdEntry[]>([]);
    
    // makes the header stick when you scroll
    const [isSticky, setIsSticky] = useState<boolean>(true);
    
    
    // this filters out the log based on requirements, you can remove any of these items to see
    // a refined collection of stdout messages
    const [logLevelEnablement, setLogLevelEnablement] = useState<string[]>(['error', 'warn', 'info', 'ok']);
    
    // ESLint can often flood stdout with WARN & ERROR due to exhaustive dependencies & rules of hooks
    // so this filter allows the removal of ESLint warnings from the collection.
    const [disableReactWarnings, setDisableReactWarnings] = useState<boolean>(false);
    
    // If you're looking for a small chunk of log messages, this is how it happens
    // you choose a from date, which will show all messages AFTER this date.
    const [fromDate, setFromDate] = useState<number>();
    
    // then if you want to limit to a certain date after, this is how it's done. 
    // this is initially left blank, as undefined will default to Date.now()
    // do not pass Date.now() into state as Date.now() gives the current time
    // this will then not fetch any newer than mount time results. 
    const [toDate, setToDate] = useState<number>();
    
    // filter query, if you're looking for a certain message or keyword
    // maybe you caught a glimpse before a reload occured, you can then 
    // type that term into the search bar in the header area, and 
    // see a list of messages containing that. 
    const [filterQuery, setFilterQuery] = useState<string>('');

    // let's add a debounce, so we don't de-dos huh ;)
    const debounce = useRef(
        new Debounce(
            (query: string) => {
                setFilterQuery(query);
            }
        )
    );
    
    // Let's face it, if you're looking purely for an exception, 
    // the last thing you want is to be looking through the entire log,
    // getting excited seeing "ERROR" to find out the likes of 
    // ESLint or another bad Log.Error() catfished you huh?
    // enable this, and avoid alcoholism :)
    const [exceptionsOnly, setExceptionsOnly] = useState<boolean>(false);
    
    // this is defaulted to 300, before it was set to 1000, but it sort of, well... it sucked.
    // there's no DB calls or anything, so what's the harm I thought...
    // That being said, I'd keep it at 300, unless you really hate yourself. 
    const [pageSize, setPageSize] = useState<int>(300 as int);
    
    // Get rid of the useless TSParenthesizedType/ TypeScript info messages. 
    const [disableTypeScriptInfo, setDisableTypeScriptInfo] = useState<boolean>(false);

    useEffect(() => {
        
        // calls the StdOutController API
        const update = () => {
            StdOutApi.getLog({
                // nullish coalescing, 
                // if no date is selected, then the latest
                // message is "NOW"
                olderThan: (toDate ?? Date.now()) as int,
                
                // 0 is 01-01-1970 00:00:00, we probably
                // don't need to go that far back, but 
                // if you're server has run for that long
                // without restarting, congratulations!
                newerThan: (fromDate ?? 0) as int,
                
                // the page size, it's set to 300, by default
                // it's just the amount of entries that come through.
                pageSize,
                
                // pass the disable react warnings variable
                disableReactWarnings,
                
                // pass a filter
                queryFilter: filterQuery,
                
                // pass the exceptions only bool
                exceptionsOnly,
                
                // now offsetting functionality yet.
                offset: 0 as int,
                
                // leave this false.
                localOnly: false,
                
                // pass the log level filter
                levels: logLevelEnablement,
                
                // disable the typescript info
                disableTypeScriptInfo
            }).then((res: string) => {
                
                // parse the response
                const response = JSON.parse(res);
                
                // reverse the array
                const results = response.results.reverse();
                
                // set the createdUtc to an actual Date object.
                results.forEach((res: StdEntry) => {
                    res.createdUtc = isoConvert(res.createdUtc);
                });
                
                // update the results
                setLog(results);
                
                // scroll to max y - 100px. 
                if (window.scrollY >= (window.scrollMaxY - 100)) {
                    window.scroll(0, window.scrollMaxY);
                }
            });
        };
        
        // call the above function
        update();
        // set an interval that calls update every 3 seconds
        const interval = setInterval(update, 3000);
        
        // when the component dismounts, clear the interval
        return () => clearInterval(interval);
    }, [
        // when any of these values mutate, this useEffect will be called again.
        logLevelEnablement,
        disableReactWarnings,
        fromDate,
        toDate,
        filterQuery,
        exceptionsOnly,
        pageSize,
        disableTypeScriptInfo
    ]);
    
    useEffect(() => {
        const evListener = () => {
            setIsSticky(window.scrollY >= 60);
        }
        
        window.addEventListener('scroll', evListener);
        
        return () => window.removeEventListener('scroll', evListener);
    })
    
    // no point repeating myself for 4 inputs. 
    // this toggles the value dependent on the key.
    const toggleLogEnablement = (key: string, checked: boolean) => {
        if (checked) {
            setLogLevelEnablement(prev => [...prev, key]);
        } else {
            setLogLevelEnablement(prev => prev.filter(item => item !== key));
        }
    };
    
    // normalize the date range, make sure the "from" date isn't passed the "to" date.
    const normalizeDateRange = (from: number, to: number) => {
        if (to < from) {
            return { from: to, to: from };
        }
        return { from, to };
    };
    
    const toInputDate = (timestamp: number | null): string => {
        if (!timestamp) return '';
        return new Date(timestamp).toISOString().split('T')[0];
    };

    const renderTag = (msg: StdEntry) => {
        const labelMap: Record<string, string> = {
            ok: 'OK',
            info: 'INFO',
            warn: 'WARN',
            error: 'ERROR',
            fatal: 'FATAL'
        };

        return (
            <span className={'tag-' + msg.type}>
                {labelMap[msg.type] || msg.type.toUpperCase()} {msg.createdUtc.toLocaleString()}
            </span>
        );
    };
    
    // in a trace, highlight any files, darken any items
    // that are not in the Api namespace or doesn't contain a CS file
    const usefulTraceItem = (trace: string) => {
        const traceLines = trace.split('\n');

        return traceLines.map((line: string, i: number) => {
            line = line.trim();
            if (line.includes('at Api.') || line.includes('.cs:line')) {
                return (
                    <p style={{ color: '#fff' }} key={i}>
                        {highlightCsFile(line)}
                    </p>
                );
            } else {
                return (
                    <p style={{ color: '#777'}}> {line}</p>
                );
            }
        });
    };
    
    // similar func for JS/TS, outputs the component files
    const usefulPathItem = (message: string) => {
        const traceLines = message.split('\n');

        return traceLines.map((line: string, i: number) => {
            const trimmed = line.trim();
            const isCodeFile = trimmed.includes('.js') || trimmed.includes('.tsx') || trimmed.includes('.ts');

            return (
                <p key={i}>
                    {isCodeFile ? highlightJsFile(trimmed) : trimmed}
                </p>
            );
        });
    }

    // HighlightFile Component (inline)
    const FileHighlight: React.FC<{ filePath: string }> = ({ filePath }) => {
        const [copied, setCopied] = useState(false);

        const handleCopy = async () => {
            try {
                await navigator.clipboard.writeText(filePath);
                setCopied(true);
            } catch (err) {
                console.error('Clipboard copy failed:', err);
            }
        };

        useEffect(() => {
            if (copied) {
                const timer = setTimeout(() => setCopied(false), 3500);
                return () => clearTimeout(timer);
            }
        }, [copied]);

        return (
            <div
                onClick={handleCopy}
                title="Click to copy file path"
                className="file-highlight"
                style={{
                    cursor: 'pointer',
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: '0.5em',
                }}
            >
                <i className="fas fa-clipboard" />
                <span>{copied ? 'Copied!' : filePath}</span>
            </div>
        );
    };

    const highlightCsFile = (line: string) => {
        const parts = line.split(/\s+/).filter(Boolean);
        
        return parts.map((part, idx) => {            
            if (part.endsWith('.cs:line')) {
                const filename = part.slice(0, -5); // remove ":line"
                return (
                    <span key={idx}>
                        <FileHighlight filePath={filename} /> on line{' '}
                    </span>
                );
            }
            return <span key={idx}> {part}</span>;
        });
    };
    const highlightJsFile = (line: string) => {
        const parts = line.split(/\s+/).filter(Boolean);

        return parts.map((part, idx) => {

            const winPathFix = part.replace('\\', '/'); // normalize a path

            if (winPathFix.startsWith('UI/Source') || winPathFix.startsWith('Admin/Source')) {
                return (
                    <span key={idx}>
                        <FileHighlight filePath={part} />
                    </span>
                );
            }
            return <span key={idx}> {part}</span>;
        });
    };

    return (
        <div>
            <Tile>
                <Alert type="info">
                    The following log is partially realtime. It will poll the current server for its latest log entries every 3 seconds.
                </Alert>
            </Tile>

            <Tile className={"stdout-controls" + (isSticky ? ' scrolled' : '')}>
                <div className="group">
                    <p>Log Level</p>
                    {['error', 'ok', 'warn', 'info'].map(level => (
                        <Input
                            key={level}
                            type="checkbox"
                            defaultChecked={logLevelEnablement.includes(level)}
                            label={level.charAt(0).toUpperCase() + level.slice(1)}
                            onChange={e => toggleLogEnablement(level, (e.target as HTMLInputElement).checked)}
                        />
                    ))}
                </div>

                <div className="group">
                    <p>JS/TS & Tooling</p>
                    <Input
                        type="checkbox"
                        defaultChecked={disableReactWarnings}
                        label="Hide react ESLint warnings"
                        onChange={e => setDisableReactWarnings((e.target as HTMLInputElement).checked)}
                    />
                    <Input
                        type="checkbox"
                        defaultChecked={disableTypeScriptInfo}
                        label="Hide TypeScript Info logs"
                        onChange={e => setDisableTypeScriptInfo((e.target as HTMLInputElement).checked)}
                    />
                </div>

                <div className="group">
                    <p>Date Range</p>
                    <Input
                        type="date"
                        value={toInputDate(fromDate)}
                        label="From"
                        onChange={e => {
                            const newFrom = new Date((e.target as HTMLInputElement).value).getTime();
                            const { from, to } = normalizeDateRange(newFrom, toDate);
                            setFromDate(from);
                            setToDate(to);
                        }}
                    />
                    <Input
                        type="date"
                        value={toInputDate(toDate)}
                        label="To"
                        onChange={e => {
                            const newTo = new Date((e.target as HTMLInputElement).value).getTime();
                            const { from, to } = normalizeDateRange(fromDate, newTo);
                            setFromDate(from);
                            setToDate(to);
                        }}
                    />
                </div>

                <div className="group">
                    <p>.NET log tools</p>
                    <Input
                        type="checkbox"
                        defaultChecked={exceptionsOnly}
                        label="Exceptions only?"
                        onChange={e => setExceptionsOnly((e.target as HTMLInputElement).checked)}
                    />
                </div>

                <div className="group">
                    <p>Filter by search</p>
                    <Input
                        type="text"
                        defaultValue={filterQuery}
                        onKeyUp={ev => debounce.current?.handle(ev.currentTarget.value)}
                    />
                </div>

                <div className="group">
                    <p>Extra options</p>
                    <Input
                        type="number"
                        defaultValue={pageSize}
                        step={10}
                        label="Result limit"
                        onChange={ev => {
                            const value = parseInt((ev.target as HTMLInputElement).value);
                            if (!isNaN(value)) setPageSize(value as int);
                        }}
                    />
                </div>
            </Tile>

            <div className="dashboards-stdout">
                {log.map((entry: StdEntry, idx: number) => (
                    <div className={'log-entry'} key={idx}>
                        {renderTag(entry)}
                        {entry.messages.map((message: StdOutMessage, i: number) =>
                            message.trace ? (
                                <div key={i}>
                                    <pre>{message.entry} <br />{usefulTraceItem(message.trace)}</pre>
                                </div>
                            ) : (
                                <p key={i}>{usefulPathItem(message.entry)}</p>
                            )
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
};

export default StdOut;
