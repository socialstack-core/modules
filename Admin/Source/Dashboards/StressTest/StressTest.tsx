import { useState, useEffect } from 'react';
import ws from 'UI/Functions/WebSocket';
import Alert from 'UI/Alert';

function requestFor(writer: ws.Writer, cType: number, id: number) {
  writer.writeByte(5);
  writer.writeUInt32(8);
  writer.writeUInt32(cType);
  writer.writeUInt32(id);
}

function blockRequest(count: number, cType: number, id: number): ws.Writer {
  const writer = new ws.Writer();

  for (let i = 0; i < count; i++) {
    requestFor(writer, cType, id);
  }

  return writer;
}

const wsStages: number[] = [50, 100, 500, 1000, 5000, 25000, 50000, 100000];

const WsStressTest: React.FC = (): React.ReactNode => {
  const [running, setRunning] = useState<any | null>(null);
  const [results, setResults] = useState<{ score: number; target: number; pass: boolean }[] | null>(null);
  const [score, setScore] = useState<number | null>(null);
  const [count, setCount] = useState<number>(0);

  const websocketAdvance = (runResults?: { score: number; target: number; pass: boolean }) => {
    let stage = 0;

    if (running) {
      stage = running.stage + 1;
    }

    if (runResults) {
      setResults(prevResults => {
        const newResults = prevResults ? [...prevResults] : [];
        newResults.push(runResults);
        return newResults;
      });

      if (!runResults.pass) {
        setRunning(null);
        setScore(Math.floor(runResults.score * runResults.target));
        return;
      }
    }

    setRunning(runWebsocket(wsStages, stage));
  };

  const runWebsocket = (stages: number[], stageIndex: number) => {
    const rps = stages[stageIndex];
    const blockSize = rps / 20;
    const writer = blockRequest(blockSize, 1, 1);
    let c = 0;
    let total = 0;
    let target = 0;

    const onGotMessage = () => {
      c++;
      total++;
    };

    ws.getSocket().addEventListener('message', onGotMessage);

    const sendI = setInterval(() => {
      ws.send(writer);
    }, 50);

    const countU = setInterval(() => {
      setCount(c);
      c = 0;
      target += rps;
    }, 1000);

    let _stopped = false;

    const stop = () => {
      if (_stopped) {
        return;
      }
      _stopped = true;
      clearInterval(sendI);
      clearInterval(countU);
      endI && clearTimeout(endI);
      ws.getSocket().removeEventListener('message', onGotMessage);
    };

    const endI = setTimeout(() => {
      stop();
      let finalScore = total / target;

      if (finalScore < 0) {
        finalScore = 0;
      } else if (finalScore > 1) {
        finalScore = 1;
      }

      // Scores 95% or higher, proceed to the next stage.
      websocketAdvance({
        score: finalScore,
        target: rps,
        pass: finalScore > 0.95,
      });
    }, 5000);

    return {
      intervals: [endI, countU, sendI],
      stage: stageIndex,
      target: rps,
      stop,
    };
  };

  return (
    <div>
      {running ? (
        <p>
          <center>
            <h1>{count}</h1>
            <h2>Responses received in the last second (5 second runtime)</h2>
            <p>{`Target is ${running.target}+`}</p>
            {results &&
              results.map((result, index) => (
                <div key={index}>
                  <span style={{ color: 'green' }}>{result.target} Passed</span> {(result.score * 100).toFixed(1)}%
                </div>
              ))}
          </center>
        </p>
      ) : (
        <>
          {score ? (
            <>
              <h2>{score} RPS</h2>
              {score < 5000 && (
                <Alert type="info">{`Lower scores are typically a measurement of your database performance. Try turning the cache on and go again.`}</Alert>
              )}
              {score > 5000 && score < 50000 && (
                <Alert type="info">
                  {`Scores in this range indicate the cache is on but the client was not able to get the throughput it needs. Confirm this by checking the CPU usage chart of your API - if it was idle throughout the test, this client was the limiting factor. Use more at once.`}
                </Alert>
              )}
            </>
          ) : null}
          <button className="btn btn-primary" onClick={() => websocketAdvance()}>
            Websocket GET stress test (experimental builds only)
          </button>
        </>
      )}
    </div>
  );
};

const StressTest: React.FC<{}> = (): React.ReactNode => {
  return (
    <p>
      <center>
        <WsStressTest />
      </center>
    </p>
  );
}

export default StressTest;
