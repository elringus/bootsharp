export interface Assembly {
    name: string;
    data: Uint8Array;
}
export declare function initializeMono(assemblies: Assembly[]): void;
export declare function callEntryPoint(assemblyName: string): Promise<any>;
