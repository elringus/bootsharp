﻿// @ts-ignore (resolved when building C# solution)
import * as bindings from "./bootsharp-bindings";
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
    return serialize(get<Invokable>(endpoint)(...deserialize(args)));
}

function invokeVoid(endpoint: string, args?: string[]): void {
    get<VoidInvokable>(endpoint)(...deserialize(args));
}

async function invokeAsync(endpoint: string, args?: string[]): Promise<string> {
    return serialize(await get<AsyncInvokable>(endpoint)(...deserialize(args)));
}

async function invokeVoidAsync(endpoint: string, args?: string[]): Promise<void> {
    return get<AsyncVoidInvokable>(endpoint)(...deserialize(args));
}

function broadcast(endpoint: string, args?: string[]): void {
    get<Event<[unknown?]>>(endpoint).broadcast(...deserialize(args));
}

function get<T extends Binding>(endpoint: string): T {
    return (cached.get(endpoint) ?? cache(endpoint)) as T;
}

function cache(endpoint: string): Binding {
    const binding = endpoint.split(".").reduce((x, y) => x[y], bindings);
    cached.set(endpoint, binding);
    return binding;
}

function deserialize(args?: string[]): unknown[] | never {
    if (args == null || args.length === 0) return null as never;
    const deserialized = new Array<unknown>(args.length);
    for (let i = 0; i < args.length; i++)
        deserialized[i] = JSON.parse(args[i]);
    return deserialized;
}

function serialize(obj: unknown): string {
    return JSON.stringify(obj);
}