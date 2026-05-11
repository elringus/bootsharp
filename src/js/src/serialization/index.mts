import { serialize, deserialize, binary } from "./serializer.mjs";
import { std } from "./std.mjs";

const serialization: {
    serialize: typeof serialize;
    deserialize: typeof deserialize;
    binary: typeof binary;
    std: typeof std;
    [factoryId: string]: unknown;
} = { serialize, deserialize, binary, std };

export default serialization;
