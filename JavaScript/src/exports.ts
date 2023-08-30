import { RuntimeAPI } from "./dotnet-types";

type Exports = {
    Invokable: Invokable;
};

type Invokable = {
    Invoke: (endpoint: string, args?: string[]) => string;
    InvokeVoid: (endpoint: string, args?: string[]) => void;
    InvokeAsync: (endpoint: string, args?: string[]) => Promise<string>;
    InvokeVoidAsync: (endpoint: string, args?: string[]) => Promise<void>;
};

let cs: Invokable;

export async function bindExports(runtime: RuntimeAPI) {
    cs = (await runtime.getAssemblyExports("Bootsharp") as Exports).Invokable;
}

export function invoke(endpoint: string, ...args: unknown[]): unknown {
    return deserialize(cs.Invoke(endpoint, serialize(args)));
}

export function invokeVoid(endpoint: string, ...args: unknown[]): void {
    cs.InvokeVoid(endpoint, serialize(args));
}

export async function invokeAsync(endpoint: string, ...args: unknown[]): Promise<unknown> {
    return deserialize(await cs.InvokeAsync(endpoint, serialize(args)));
}

export function invokeVoidAsync(endpoint: string, ...args: unknown[]): Promise<void> {
    return cs.InvokeVoidAsync(endpoint, serialize(args));
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
