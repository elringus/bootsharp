/// <reference types="vite/client" />

import { bootBackend } from "boot";
import { createRoot } from "react-dom/client";
import { createElement } from "react";
import "index.css";

await bootBackend();

createRoot(document.getElementById("app")!).render([
    createElement(require("donut"), 33333),
    createElement(require("computer"), 60)
]);
