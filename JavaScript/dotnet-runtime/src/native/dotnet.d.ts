export interface DotNetModule extends EmscriptenModule {
    MONO: any,
    BINDING: any,
    ccall(ident: string,
        returnType: Emscripten.JSType | null,
        argTypes: Emscripten.JSType[],
        args: Emscripten.TypeCompatibleWithC[],
        opts?: Emscripten.CCallOpts): any;
}

declare var factory: EmscriptenModuleFactory<DotNetModule>;

export default factory;
