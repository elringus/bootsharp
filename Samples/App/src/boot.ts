import backend, { BinaryResource } from "backend";

export async function bootBackend() {
    await Promise.all([
        fetchContent(backend.resources.wasm),
        ...backend.resources.assemblies.map(fetchContent)
    ]);
    await backend.boot();
}

async function fetchContent(resource: BinaryResource) {
    const uri = `/bin/${resource.name}`;
    resource.content = new Uint8Array(await (await fetch(uri)).arrayBuffer());
}
