import type { RuntimeAPI } from "./dotnet.d.ts";

type Exports = {
    Bootsharp: {
        Invokable: Invokable;
    }
};

type Invokable = {
    Invoke: (endpoint: string, args?: string[]) => string;
    InvokeVoid: (endpoint: string, args?: string[]) => void;
    InvokeAsync: (endpoint: string, args?: string[]) => Promise<string>;
    InvokeVoidAsync: (endpoint: string, args?: string[]) => Promise<void>;
};

let invokable: Invokable;

export async function bindExports(runtime: RuntimeAPI) {
    invokable = (await runtime.getAssemblyExports("Bootsharp") as Exports).Bootsharp.Invokable;
}

export function invoke(endpoint: string, ...args: unknown[]): unknown {
    const result = invokable.Invoke(endpoint, serialize(args));
    return deserialize(result);
}

export function invokeVoid(endpoint: string, ...args: unknown[]): void {
    invokable.InvokeVoid(endpoint, serialize(args));
}

export async function invokeAsync(endpoint: string, ...args: unknown[]): Promise<unknown> {
    const result = await invokable.InvokeAsync(endpoint, serialize(args));
    return deserialize(result);
}

export function invokeVoidAsync(endpoint: string, ...args: unknown[]): Promise<void> {
    return invokable.InvokeVoidAsync(endpoint, serialize(args));
}

function serialize(args: unknown[]): string[] | undefined {
    if (args == null || args.length === 0) return undefined;
    const serialized = new Array<string>(args.length);
    for (let i = 0; i < args.length; i++)
        serialized[i] = JSON.stringify(args[i]);
    return serialized;
}

function deserialize(json: string): unknown {
    return JSON.parse(json);
}
