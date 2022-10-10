export function injectCrypto(): void {
    if (typeof crypto !== "undefined" && "getRandomValues" in crypto) return;
    globalThis.crypto = {
        getRandomValues: getRandomValues,
        randomUUID: randomUUID
    } as any;
}

function getRandomValues(buffer: Uint8Array): Uint8Array {
    for (let i = 0; i < buffer.length; i++)
        buffer[i] = (Math.random() * 256) | 0;
    return buffer;
}

function randomUUID() {
    return (
        [1e7] as any + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g,
        c => (c ^ getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
    );
}
