import { RuntimeAPI } from "./dotnet";

export function bindImports(runtime: RuntimeAPI) {
    runtime.setModuleImports("Bootsharp", {
        invoke: (endpoint: string, args?: string[]): string => {
            return "";
        },
        invokeVoid: (endpoint: string, args?: string[]): void => {

        },
        invokeAsync: async (endpoint: string, args?: string[]): Promise<string> => {
            return "";
        },
        invokeVoidAsync: async (endpoint: string, args?: string[]): Promise<void> => {

        },
        broadcast: (endpoint: string, args?: string[]): void => {

        }
    });
}
