import { RuntimeAPI } from "./dotnet-types";

type SerializedExports = {
    Invokable: {
        Invoke: (endpoint: string, args?: string[]) => string;
        InvokeVoid: (endpoint: string, args?: string[]) => void;
        InvokeAsync: (endpoint: string, args?: string[]) => Promise<string>;
        InvokeVoidAsync: (endpoint: string, args?: string[]) => Promise<void>;
    };
};

export async function bindExports(runtime: RuntimeAPI) {
    const exports: SerializedExports = await runtime.getAssemblyExports("Bootsharp");

}
