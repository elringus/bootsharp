import { Event } from "./event.mjs";
import { bindImports as bindGeneratedImports } from "./generated/imports.g.mjs";
import { instances } from "./instances.mjs";
import type { RuntimeAPI } from "./dotnet/index.mjs";

export function bindImports(runtime: RuntimeAPI) {
    bindGeneratedImports(runtime);
    runtime.setModuleImports("Bootsharp", { instances });
}

// noinspection JSUnusedGlobalSymbols (used by the generated code)
export function importEvent<T extends unknown[]>(handler: (...args: [...T]) => void): Event<T> {
    const event = new Event<T>();
    const broadcast = event.broadcast.bind(event);
    event.broadcast = (...args: [...T]) => {
        broadcast(...args);
        handler(...args);
    };
    return event;
}

// noinspection JSUnusedGlobalSymbols (used by the generated code in debug mode)
export function getImport<T>(handler: unknown, serializedHandler: T, name: string): T {
    if (typeof handler !== "function")
        throw Error(`Failed to invoke '${name}' from C#. Make sure to assign the function in JavaScript.`);
    return serializedHandler;
}
