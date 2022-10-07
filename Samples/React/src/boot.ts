import { boot, BootData, getBootUris } from "backend";

export async function bootBackend() {
    const bootData = await fetchBootData();
    await boot(bootData);
}

async function fetchBootData(): Promise<BootData> {
    const uris = getBootUris()!;
    return {
        wasm: await fetchBinary(uris.wasm),
        assemblies: await Promise.all(uris.assemblies.map(fetchAssembly)),
        entryAssemblyName: uris.entryAssembly
    };

    async function fetchBinary(name: string) {
        const uri = `${process.env.PUBLIC_URL}/bin/${name}`;
        return new Uint8Array(await (await fetch(uri)).arrayBuffer());
    }

    async function fetchAssembly(name: string) {
        return { name, data: await fetchBinary(name) };
    }
}
