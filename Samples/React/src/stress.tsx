import { useEffect, useState } from "react";
import { Backend } from "backend";

type Props = {
    power: number;
};

export const Stress = (props: Props) => {
    const [stressing, setStressing] = useState(true);
    const [resolving, setResolving] = useState(false);
    const [iterations, setIterations] = useState("");

    useEffect(() => {
        Backend.GetStressPower = async () => props.power;
    }, [props.power]);

    useEffect(() => {
        void Backend.OnStressIteration.subscribe(logIteration);
        void Backend.StartStress();
        return () => {
            void Backend.OnStressIteration.unsubscribe(logIteration);
            void Backend.StopStress();
        };
    }, []);

    return (
        <div id="stress">
            <div>
                This sample shows the benefit of running dotnet on worker thread.
                The Donut is animating on the main (UI) thread, while dotnet is running stress test.
                When built without 'CreateWorker' enabled, the animation will have a poor FPS.
            </div>
            <button onClick={toggleStress} disabled={resolving}>
                {getButtonText()}
            </button>
            <div id="iterations">
                {iterations}
            </div>
        </div>
    );

    async function toggleStress() {
        setResolving(true);
        if (await Backend.IsStressing())
            await Backend.StopStress();
        else await Backend.StartStress();
        setResolving(false);
        setStressing(!stressing);
    }

    function getButtonText() {
        if (resolving) return "RESOLVING...";
        return stressing ? "STOP STRESS" : "START STRESS";
    }

    function logIteration(time: number) {
        setIterations(i => {
            if (i.length > 999) i = i.substring(0, i.lastIndexOf("\n"));
            const stamp = new Date().toLocaleTimeString([], { hour12: false });
            return `[${stamp}] Stressed over ${time}ms.\n${i}`;
        });
    }
};
