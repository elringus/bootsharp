import { useState, useEffect } from "react";

type Props = {
    fps: number;
};

export default ({ fps }: Props) => {
    const [time, setTime] = useState<number>();

    useEffect(() => {
        const delay = 1000 / fps;
        const handle = setInterval(() => setTime(Date.now()), delay);
        return () => clearInterval(handle);
    }, [fps]);

    return <div id="donut" style={{ transform: `rotate(${time}deg)` }}/>;
};
