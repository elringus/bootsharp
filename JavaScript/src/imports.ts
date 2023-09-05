// @ts-expect-error (resolved when building C# solution)
import * as bindings from "./bindings.g";
import type { RuntimeAPI } from "./dotnet.d.ts";
import type { Event } from "./event";

type Binding = Invokable | VoidInvokable | AsyncInvokable | AsyncVoidInvokable | Event<[unknown?]>;
type Invokable = (...args: unknown[]) => unknown;
type VoidInvokable = (...args: unknown[]) => void;
type AsyncInvokable = (...args: unknown[]) => Promise<unknown>;
type AsyncVoidInvokable = (...args: unknown[]) => Promise<void>;

const cached = new Map<string, Binding>;

export function bindImports(runtime: RuntimeAPI) {
    runtime.setModuleImports("Bootsharp", { invoke, invokeVoid, invokeAsync, invokeVoidAsync, broadcast });
}

function invoke(endpoint: string, args?: string[]): string {
    const invokable = get<Invokable>(endpoint);
    const result = args == null ? invokable() : invokable(...deserialize(args));
    return serialize(result);
}

function invokeVoid(endpoint: string, args?: string[]): void {
    const invokable = get<VoidInvokable>(endpoint);
    if (args == null) invokable();
    else invokable(...deserialize(args));
}

async function invokeAsync(endpoint: string, args?: string[]): Promise<string> {
    const invokable = get<AsyncInvokable>(endpoint);
    const result = await (args == null ? invokable() : invokable(...deserialize(args)));
    return serialize(result);
}

async function invokeVoidAsync(endpoint: string, args?: string[]): Promise<void> {
    const invokable = get<AsyncVoidInvokable>(endpoint);
    if (args == null) await invokable();
    else await invokable(...deserialize(args));
}

function broadcast(endpoint: string, args?: string[]): void {
    const event = get<Event<[unknown?]>>(endpoint);
    if (args == null) event.broadcast();
    else event.broadcast(...deserialize(args));
}

function get<T extends Binding>(endpoint: string): T {
    return (cached.get(endpoint) ?? resolve(endpoint)) as T;
}

function resolve(endpoint: string): Binding {
    const binding = endpoint.split(".").reduce((x, y) => x[y], bindings);
    if (binding == null) throw Error(`'${endpoint}' JavaScript endpoint is not bind.`);
    cached.set(endpoint, binding);
    return binding;
}

function deserialize(args: string[]): unknown[] {
    const deserialized = new Array<unknown>(args.length);
    for (let i = 0; i < args.length; i++)
        deserialized[i] = JSON.parse(args[i]);
    return deserialized;
}

function serialize(obj: unknown): string {
    return JSON.stringify(obj);
}
