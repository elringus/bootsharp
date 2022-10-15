/// <reference types="react-scripts" />

import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { bootBackend } from "boot";
import { Donut } from "donut";
import { Stress } from "stress";
import "index.css";

(async function () {
    await bootBackend();
    await renderApp();
})();

async function renderApp() {
    const container = document.getElementById("react-app")!;
    await createRoot(container).render(
        <StrictMode>
            <Stress power={33333}/>
            <Donut fps={60}/>
        </StrictMode>
    );
}
