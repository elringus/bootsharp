import bootsharp, { Bench } from "./cs/bin/bootsharp/index.mjs";

const record1 = {
    string: "1", bool: true, int: 1, double: 1.0, enum: Bench.Enum.Foo, record: {
        doubles: new Float64Array([1.0, 1.1, 1.2]),
        ints: new Int32Array([1, 2, 3]),
        strings: ["foo", "bar", "baz"],
        enums: [Bench.Enum.Foo, Bench.Enum.Bar, Bench.Enum.Baz]
    }
};

const record2 = {
    string: "2", bool: false, int: -999, double: 0.000001, enum: Bench.Enum.Bar, record: {
        doubles: new Float64Array([-99999.0, 99999.0, -0.000001]),
        ints: new Int32Array([-1, 0, -2, 0, -3, 999999, -999999]),
        strings: ["Lorem ipsum dolor sit amet.", "Consectetur adipiscing elit."],
        enums: [Bench.Enum.Bar, Bench.Enum.Baz]
    }
};

await bootsharp.boot();

for (let i = 0; i < 999; i++)
    Bench.Program.echo({
        list: [record1, record2],
        roList: [record2, record1],
        dict: new Map([["1", record1], ["2", record2]])
    });
