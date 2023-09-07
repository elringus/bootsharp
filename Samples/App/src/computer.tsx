import { useEffect, useState, useCallback } from "react";
import { Computer, PrimeComputerUI } from "backend";

type Props = {
    complexity: number;
    resultLimit: number;
};

export default ({ complexity, resultLimit }: Props) => {
    const [computing, setComputing] = useState(false);
    const [results, setResults] = useState("");

    const toggle = useCallback(() => {
        if (Computer.isComputing()) Computer.stopComputing();
        else Computer.startComputing();
        setComputing(!computing);
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
        PrimeComputerUI.getComplexity = () => complexity;
    }, [complexity]);

    useEffect(() => {
        PrimeComputerUI.onComplete.subscribe(logResult);
        return () => PrimeComputerUI.onComplete.unsubscribe(logResult);
    }, [logResult]);

    return (
        <div id="computer">
            <div>
                This sample shows the benefit of AOT-compiling the C# backend.
                The Donut is animating on the main (UI) thread while backend is computing.
                With AOT enabled, compute complexity can be higher w/o affecting the animation.
            </div>
            <button onClick={toggle}>
                {computing ? "STOP COMPUTE" : "START COMPUTE"}
            </button>
            <div id="results">
                {results}
            </div>
        </div>
    );
};
