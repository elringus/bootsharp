import backend from "backend";
import react from "react-dom/client";
import Computer from "./computer";
import Donut from "./donut";
import "./index.css";

backend.resources.root = "/bin";
await backend.boot();

react.createRoot(document.getElementById("app")!).render(<>
    <Donut fps={60}/>
    <Computer complexity={25000} resultLimit={9}/>
</>);
