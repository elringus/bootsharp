import { useEffect, useState, useCallback } from "react";
import { Backend, Frontend } from "backend";

type Props = {
    complexity: number;
};

export default ({ complexity }: Props) => {
    const [computing, setComputing] = useState(false);
    const [results, setResults] = useState("");

    const toggle = useCallback(() => {
        if (Backend.isComputing()) Backend.stopComputing();
        else Backend.startComputing();
        setComputing(!computing);
    }, [computing]);

    const logResult = useCallback((time: number) => {
        setResults(i => {
            if (i.length > 999) i = i.substring(0, i.lastIndexOf("\n"));
            const stamp = new Date().toLocaleTimeString([], { hour12: false });
            return `[${stamp}] Computed in ${time}ms.\n${i}`;
        });
    }, []);

    useEffect(() => {
        Frontend.getComplexity = () => complexity;
    }, [complexity]);

    useEffect(() => {
        Frontend.onComplete.subscribe(logResult);
        return () => Frontend.onComplete.unsubscribe(logResult);
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
