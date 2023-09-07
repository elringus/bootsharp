import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

export default defineConfig({
    plugins: [react()],
    // Ignore node-specific calls in .NET's JavaScript:
    // https://github.com/dotnet/runtime/issues/91558.
    build: { rollupOptions: { external: ["process", "module"] } }
});
