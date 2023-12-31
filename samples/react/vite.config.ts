/// <reference types="vitest"/>

import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

export default defineConfig({
    plugins: [react()],
    test: {
        environment: "happy-dom",
        setupFiles: ["test/setup.ts"],
        coverage: { include: ["src/**"] }
    },
    server: {
        headers: {
            "Cross-Origin-Opener-Policy": "same-origin",
            "Cross-Origin-Embedder-Policy": "require-corp"
        }
    },
    build: {
        target: "chrome89",
        // Ignore node-specific calls in .NET's JavaScript:
        // https://github.com/dotnet/runtime/issues/91558.
        rollupOptions: { external: ["process", "module"] }
    }
});
