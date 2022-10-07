/// <reference types="react-scripts" />

import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { bootBackend } from "boot";
import { Donut } from "donut";
import "index.css";

(async function () {
    await bootBackend();
    await renderApp();
})();

async function renderApp() {
    const container = document.getElementById("react-app")!;
    await createRoot(container).render(
        <StrictMode>
            <Donut delay={18} prime={6666}/>
        </StrictMode>
    );
}
