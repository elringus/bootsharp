/// <reference types="emscripten" />
export declare let wasm: DotNetModule;
export interface DotNetModule extends EmscriptenModule {
    MONO: any;
    BINDING: any;
    ccall(ident: string, returnType: Emscripten.JSType | null, argTypes: Emscripten.JSType[], args: Emscripten.TypeCompatibleWithC[], opts?: Emscripten.CCallOpts): any;
}
export declare function initializeWasm(wasmBinary: Uint8Array): Promise<void>;
export declare function destroyWasm(): void;
