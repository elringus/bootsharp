import { useState, useEffect } from "react";
import { useEvent } from "utilities";
import { Backend } from "backend";

type Props = {
    delay: number;
    prime: number;
};

export const Donut = (props: Props) => {
    const [time, setTime] = useState<number>();

    useEffect(() => {
        const handle = setInterval(() => setTime(Date.now()), props.delay);
        return () => clearInterval(handle);
    }, [props.delay]);

    useEffect(() => {
        Backend.GetStressPower = async () => props.prime;
    }, [props.prime]);

    useEffect(() => {
        void Backend.StartStress();
        return () => void Backend.StopStress();
    }, []);

    useEvent(Backend.OnStressIteration, console.log, []);

    return <div className="donut"
                onClick={toggleStress}
                style={{ transform: `rotate(${time}deg)` }}/>;

    async function toggleStress() {
        if (await Backend.IsStressing())
            await Backend.StopStress();
        else await Backend.StartStress();
    }
};
