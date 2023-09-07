/// <reference types="vite/client" />

import { createRoot } from "react-dom/client";
import { bootBackend } from "./boot";
import Computer from "./computer";
import Donut from "./donut";
import "./index.css";

bootBackend().then(() =>
    createRoot(document.getElementById("app")!).render(<>
        <Donut fps={60}/>
        <Computer complexity={25000} resultLimit={9}/>
    </>));
