import { useEffect, useState, useCallback, ChangeEvent } from "react";
import { Computer, Backend, PrimeUI } from "backend";

type Props = {
    options: Backend.Prime.Options;
    resultLimit: number;
};

export default ({ options, resultLimit }: Props) => {
    const [computing, setComputing] = useState(false);
    const [complexity, setComplexity] = useState(options.complexity);
    const [multithreading, setMultithreading] = useState(options.multithreading);
    const [results, setResults] = useState("");

    const toggleCompute = useCallback(() => {
        if (Computer.isComputing()) Computer.stopComputing();
        else Computer.startComputing();
        setComputing(!computing);
    }, [computing]);

    const toggleMultithreading = useCallback(() => {
        setMultithreading(!multithreading);
        if (computing) Computer.startComputing();
    }, [multithreading, computing]);

    const handleComplexityInputChange = useCallback((event: ChangeEvent<HTMLInputElement>) => {
        setComplexity(event.target.valueAsNumber);
        if (computing) Computer.startComputing();
    }, [computing]);

    const logResult = useCallback((time: number) => {
        setResults(i => {
            if ((i.match(/\n/g)?.length ?? 0) > resultLimit)
                i = i.substring(0, i.lastIndexOf("\n"));
            const stamp = new Date().toLocaleTimeString([], { hour12: false });
            return `[${stamp}] Computed in ${time}ms.\n${i}`;
        });
    }, []);

    useEffect(() => {
        PrimeUI.getOptions = () => ({ complexity, multithreading });
    }, [complexity, multithreading]);

    useEffect(() => {
        PrimeUI.onComplete.subscribe(logResult);
        return () => PrimeUI.onComplete.unsubscribe(logResult);
    }, [logResult]);

    return (
        <div id="computer">
            <div>
                The Donut is animating on the UI thread, while a background thread computing.
                When MULTITHREADING disabled, the computation will run on UI affecting animation.
            </div>
            <div id="controls">
                <label>
                    COMPLEXITY
                    <input type="number" value={complexity} onChange={handleComplexityInputChange}/>
                </label>
                <label>
                    MULTITHREADING
                    <input type="checkbox" checked={multithreading} onChange={toggleMultithreading}/>
                </label>
            </div>
            <button onClick={toggleCompute}>
                {computing ? "STOP COMPUTE" : "START COMPUTE"}
            </button>
            <div id="results">
                {results}
            </div>
        </div>
    );
};
