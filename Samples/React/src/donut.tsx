import { useState, useEffect } from "react";
import { Backend } from "backend";

type Props = {
    delay: number;
    prime: number;
};

export const Donut = (props: Props) => {
    Backend.GetPrime = async () => props.prime;
    Backend.Log = console.log;
    Backend.OnWarn.subscribe(m => console.log("lololol" + m));

    const [time, setTime] = useState<number>();
    const [stressing, setStressing] = useState<boolean>(true);

    useEffect(() => {
        const handle = setInterval(() => setTime(Date.now()), props.delay);
        return () => clearInterval(handle);
    }, [props.delay]);

    useEffect(() => {
        void stress();
        return () => setStressing(false);
    }, [props.prime]);

    return <div className="donut" style={{ transform: `rotate(${time}deg)` }}/>;

    async function stress() {
        while (stressing) {
            await Backend.ComputePrime();
            await new Promise(r => setTimeout(r, 1));
        }
    }
};
