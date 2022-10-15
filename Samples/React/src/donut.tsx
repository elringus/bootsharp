import { useState, useEffect } from "react";

type Props = {
    fps: number;
};

export const Donut = (props: Props) => {
    const [time, setTime] = useState<number>();

    useEffect(() => {
        const delay = 1000 / props.fps;
        const handle = setInterval(() => setTime(Date.now()), delay);
        return () => clearInterval(handle);
    }, [props.fps]);

    return <div id="donut" style={{ transform: `rotate(${time}deg)` }}/>;
};
