import backend, { BinaryResource } from "backend";

export async function bootBackend() {
    await Promise.all([
        fetchBin(backend.resources.wasm),
        ...backend.resources.assemblies.map(fetchBin)
    ]);
    await backend.boot();
}

async function fetchBin(resource: BinaryResource) {
    const uri = `${import.meta.env.BASE_SERVER}/bin/${resource.name}`;
    resource.content = new Uint8Array(await (await fetch(uri)).arrayBuffer());
}
