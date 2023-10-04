/// <reference types="vite/client" />

import { createRoot } from "react-dom/client";
import backend from "backend";
import Computer from "./computer";
import Donut from "./donut";
import "./index.css";

backend.resources.root = "/bin";
backend.boot().then(() =>
    createRoot(document.getElementById("app")!).render(<>
        <Donut fps={60}/>
        <Computer complexity={25000} resultLimit={9}/>
    </>));
