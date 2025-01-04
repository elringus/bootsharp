const lookup = new Uint8Array([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 62, 0, 62, 0, 63, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 0, 0, 0, 0, 63, 0, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51]);

export function decodeBase64(source: string): ArrayBuffer {
    if (typeof window === "object") return decodeWithBrowser(source);
    if (typeof process === "object") return decodeWithNode(source);
    return decodeNaive(source);
}

function decodeWithBrowser(source: string): ArrayBuffer {
    const binaryString = window.atob(source);
    const length = binaryString.length;
    const buffer = new ArrayBuffer(length);
    const uint8Array = new Uint8Array(buffer);
    for (let i = 0; i < length; i++)
        uint8Array[i] = binaryString.charCodeAt(i);
    return buffer;
}

function decodeWithNode(source: string): ArrayBuffer {
    const buffer = Buffer.from(source, "base64");
    return buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength);
}

function decodeNaive(source: string): ArrayBuffer {
    const srcLen = source.length;
    const padLen = (source[srcLen - 2] === "=" ? 2 : (source[srcLen - 1] === "=" ? 1 : 0));
    const outLen = ((srcLen - padLen) * 3) >> 2;
    const buffer = new Uint8Array(outLen);

    let tmp;
    let byteIndex = 0;

    for (let i = 0, baseLen = srcLen - padLen; i < baseLen; i += 4) {
        tmp = (lookup[source.charCodeAt(i)] << 18)
            | (lookup[source.charCodeAt(i + 1)] << 12)
            | (lookup[source.charCodeAt(i + 2)] << 6)
            | (lookup[source.charCodeAt(i + 3)]);
        buffer[byteIndex++] = (tmp >> 16) & 0xFF;
        buffer[byteIndex++] = (tmp >> 8) & 0xFF;
        buffer[byteIndex++] = tmp & 0xFF;
    }

    if (padLen === 1) {
        tmp = (lookup[source.charCodeAt(srcLen - 4)] << 18)
            | (lookup[source.charCodeAt(srcLen - 3)] << 12)
            | (lookup[source.charCodeAt(srcLen - 2)] << 6);
        buffer[byteIndex++] = (tmp >> 16) & 0xFF;
        buffer[byteIndex++] = (tmp >> 8) & 0xFF;
    } else if (padLen === 2) {
        tmp = (lookup[source.charCodeAt(srcLen - 4)] << 18)
            | (lookup[source.charCodeAt(srcLen - 3)] << 12);
        buffer[byteIndex++] = (tmp >> 16) & 0xFF;
    }

    return buffer.buffer;
}
