/// <reference types="vite/client" />

import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { bootBackend } from "./boot";
import { Donut } from "./donut";
import { Stress } from "./stress";
import "index.css";

await bootBackend();

createRoot(document.getElementById("react-app")!).render(
    <StrictMode>
        <Stress power={33333}/>
        <Donut fps={60}/>
    </StrictMode>
);
