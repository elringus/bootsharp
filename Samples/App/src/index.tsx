/// <reference types="vite/client" />

import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { bootBackend } from "./boot";
import { Donut } from "./donut";
import { Prime } from "./prime";
import "index.css";

await bootBackend();

createRoot(document.getElementById("app")!).render(
    <StrictMode>
        <Prime complexity={33333}/>
        <Donut fps={60}/>
    </StrictMode>
);
