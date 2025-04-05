// TODO: Figure how to get fixtures from "../fixtures.mjs"

mergeInto(LibraryManager.library, {
    getNumber: () => 42,
    getStruct: () => {
        const data = {
            info: "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
            ok: true,
            revision: -112,
            messages: ["foo", "bar", "baz", "nya", "far"]
        };
        const json = JSON.stringify(data);
        const size = lengthBytesUTF16(json) + 1;
        const ptr = _malloc(size);
        stringToUTF16(json, ptr, size);
        return ptr; // has to be freed after use in real use cases
    }
});
