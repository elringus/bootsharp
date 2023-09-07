import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

export default defineConfig({
    plugins: [react()],
    build: {
        target: "esnext",
        // Ignore node-specific calls in .NET's JavaScript:
        // https://github.com/dotnet/runtime/issues/91558.
        rollupOptions: { external: ["process", "module"] }
    }
});
