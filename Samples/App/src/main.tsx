import backend from "backend";
import react from "react-dom/client";
import Computer from "./computer";
import Donut from "./donut";
import "./index.css";

await backend.boot({ root: "/bin" });

react.createRoot(document.getElementById("app")!).render(<>
    <Donut fps={60}/>
    <Computer options={{ complexity: 25000, multithreading: true }} resultLimit={10}/>
</>);
