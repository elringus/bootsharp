import { RuntimeAPI } from "./dotnet-api";

type Exports = {
    Invokable: {
        Invoke: (endpoint: string, args?: string[]) => string;
        InvokeVoid: (endpoint: string, args?: string[]) => void;
        InvokeAsync: (endpoint: string, args?: string[]) => Promise<string>;
        InvokeVoidAsync: (endpoint: string, args?: string[]) => Promise<void>;
    };
};

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

export async function bindExports(runtime: RuntimeAPI) {
    const exports: Exports = await runtime.getAssemblyExports("Bootsharp");

}
