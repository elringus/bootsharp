/// <reference types="vite/client" />

import { createRoot } from "react-dom/client";
import { createElement, StrictMode } from "react";
import { bootBackend } from "./boot";
import "./index.css";

await bootBackend();

createRoot(document.getElementById("app")!).render(
    createElement(StrictMode, null,
        createElement((await import("./donut")).default, { fps: 60 }),
        createElement((await import("./computer")).default, { complexity: 33333, resultLimit: 9 })
    ));
