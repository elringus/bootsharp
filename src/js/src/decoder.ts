const lookup = new Uint8Array([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 62, 0, 62, 0, 63, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 0, 0, 0, 0, 63, 0, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51]);

export function decodeBase64(source: string): Uint8Array {
    if (typeof window === "object") return Uint8Array.from(window.atob(source), c => c.charCodeAt(0));
    if (typeof Buffer === "function") return Buffer.from(source, "base64");

    const sourceLength = source.length;
    const paddingLength = (source[sourceLength - 2] === "=" ? 2 : (source[sourceLength - 1] === "=" ? 1 : 0));
    const baseLength = (sourceLength - paddingLength) & 0xfffffffc;

    let tmp;
    let i = 0;
    let byteIndex = 0;
    const buffer = [];

    for (; i < baseLength; i += 4) {
        tmp = (lookup[source.charCodeAt(i)] << 18) | (lookup[source.charCodeAt(i + 1)] << 12) | (lookup[source.charCodeAt(i + 2)] << 6) | (lookup[source.charCodeAt(i + 3)]);
        buffer[byteIndex++] = (tmp >> 16) & 0xFF;
        buffer[byteIndex++] = (tmp >> 8) & 0xFF;
        buffer[byteIndex++] = (tmp) & 0xFF;
    }

    if (paddingLength === 1) {
        tmp = (lookup[source.charCodeAt(i)] << 10) | (lookup[source.charCodeAt(i + 1)] << 4) | (lookup[source.charCodeAt(i + 2)] >> 2);
        buffer[byteIndex++] = (tmp >> 8) & 0xFF;
        buffer[byteIndex++] = tmp & 0xFF;
    }

    if (paddingLength === 2) {
        tmp = (lookup[source.charCodeAt(i)] << 2) | (lookup[source.charCodeAt(i + 1)] >> 4);
        buffer[byteIndex++] = tmp & 0xFF;
    }

    return new Uint8Array(buffer);
}
